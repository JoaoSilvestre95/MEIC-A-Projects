package loadbalancer;

import java.util.*;

import com.amazonaws.AmazonClientException;
import com.amazonaws.AmazonServiceException;
import com.amazonaws.auth.AWSCredentials;
import com.amazonaws.auth.AWSStaticCredentialsProvider;
import com.amazonaws.auth.profile.ProfileCredentialsProvider;
import com.amazonaws.services.ec2.AmazonEC2;
import com.amazonaws.services.ec2.AmazonEC2ClientBuilder;
import com.amazonaws.services.ec2.model.TerminateInstancesRequest;
import com.amazonaws.services.ec2.model.DescribeInstancesResult;
import com.amazonaws.services.ec2.model.RunInstancesRequest;
import com.amazonaws.services.ec2.model.RunInstancesResult;
import com.amazonaws.services.ec2.model.Instance;
import com.amazonaws.services.ec2.model.Reservation;
import com.amazonaws.services.cloudwatch.AmazonCloudWatch;
import com.amazonaws.services.cloudwatch.AmazonCloudWatchClientBuilder;
import com.amazonaws.services.cloudwatch.model.Dimension;
import com.amazonaws.services.cloudwatch.model.Datapoint;
import com.amazonaws.services.cloudwatch.model.GetMetricStatisticsRequest;
import com.amazonaws.services.cloudwatch.model.GetMetricStatisticsResult;
import com.amazonaws.services.ec2.model.DescribeAvailabilityZonesResult;

import javax.crypto.Mac;

public class AutoScaler {
    private final int checkPeriod = 10;

    private AmazonEC2 ec2;
    private AmazonCloudWatch cloudWatch;

    private List<Machine> runningMachines;
    private List<Machine> bootingMachines;
    private Queue<Request> pendingRequests;

    public AutoScaler() {
        this.runningMachines = new LinkedList<Machine>();
        this.bootingMachines = new LinkedList<Machine>();
        this.pendingRequests = new LinkedList<>();
        init();
    }

    private void init() {

        AWSCredentials credentials;

        try {
            credentials = new ProfileCredentialsProvider().getCredentials();
        } catch (Exception e) {
            throw new AmazonClientException(
                    "Cannot load the credentials from the credential profiles file. " +
                            "Please make sure that your credentials file is at the correct " +
                            "location (~/.aws/credentials), and is in valid format.",
                    e);
        }
        ec2 = AmazonEC2ClientBuilder.standard().withRegion("us-east-1").withCredentials(new AWSStaticCredentialsProvider(credentials)).build();
        cloudWatch = AmazonCloudWatchClientBuilder.standard().withRegion("us-east-1").withCredentials(new AWSStaticCredentialsProvider(credentials)).build();

        Thread thread = new Thread(new Runnable() {
            public void run(){
                monitorize();
            }
        });

        thread.start();
    }
    private long getPendingLoad(){
        long load = 0;
        for (Request request: pendingRequests) {
            load += request.getWeight();
        }
        return load;
    }

    private void addPendingRequest(Request request){
        if(!this.pendingRequests.contains(request))
            this.pendingRequests.add(request);
    }

    synchronized public void requestMachines(Request request){
        int startingMachines = this.bootingMachines.size();
        addPendingRequest(request);
        long loadCovered = startingMachines * LoadBalancer.MAXLOAD;
        long loadToCover = getPendingLoad() - loadCovered;
        int machineToStart = 0;
        System.out.println("LoadToCover " + loadToCover);
        if (loadToCover > 0) {
            machineToStart = (int) Math.ceil(loadToCover/(double)LoadBalancer.MAXLOAD);
        }
        System.out.println("I need " + machineToStart + " machines ");
        for(int i = 0; i < machineToStart; i++){
            launchMachine();
        }
    }


    public void monitorize(){
        launchMachine();
        while(true) {
            try {
                Thread.sleep(checkPeriod * 1000);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
            checkBootingMachines();
            checkRunningMachines();
            logInfoSystem();
        }
    }

    private void logInfoSystem(){
        System.out.println("Current Booting Machines:");
        for (Machine m : bootingMachines){
            System.out.println(m.getInstance().getInstanceId());
        }
        System.out.println("Current Running Machines:");
        for (Machine m : runningMachines){
            System.out.print(m.getInstance().getInstanceId() + " has " + m.requests.size() + " requests ");
            //System.out.print(" and CPU usage: " + m.getCpu());
            System.out.println(" | Current Load: " + m.getMachineLoad());
        }
    }

    synchronized private void checkRunningMachines(){
        List<Machine> toRemove = new ArrayList<>();
        for (Machine machine : runningMachines){
            //machine.setCpu(calculateCpuAverage(machine));
            if(machine.getMachineLoad() == 0){
                machine.incNoWorkTimes();
                if(machine.getNoWorkTimes() == 5)
                    toRemove.add(machine);

            }
        }
        for (Machine m : toRemove){
            terminateMachine(m);
        }
    }

    synchronized private void checkBootingMachines(){
        List<Machine> toRemove = new ArrayList<Machine>();
        HashMap<String, Instance> currentInstances = fetchAwsInstances();
        for (Machine machine : bootingMachines) {
            Instance instance = currentInstances.get(machine.getInstance().getInstanceId());
            if(instance != null) {
                machine.setInstance(instance);
                if(machine.ping()){
                    toRemove.add(machine);
                }
            }
        }
        for (Machine machine : toRemove) {
            this.bootingMachines.remove(machine);
            this.runningMachines.add(machine);
        }

        if(toRemove.size() > 0){//Wake a pending request if a new machine started to run
            removeRequest();
        }

    }
    synchronized public void removeRequest(){
        if(this.pendingRequests.size() > 0){
            Request request = this.pendingRequests.poll();
            request.wake();
        }

    }
    private HashMap<String, Instance> fetchAwsInstances(){
        DescribeInstancesResult describeInstancesRequest = ec2.describeInstances();
        List<Reservation> reservations = describeInstancesRequest.getReservations();
        HashSet<Instance> instances = new HashSet<Instance>();
        HashMap<String, Instance> instancesMap= new HashMap<>();

        for (Reservation reservation : reservations) {
            instances.addAll(reservation.getInstances());
        }

        for (Instance instance : instances){
            instancesMap.put(instance.getInstanceId(), instance);
        }
        return instancesMap;
    }

    private double calculateCpuAverage(Machine machine){
        List<Datapoint> datapoints = calculateCpuUsage(machine);
        Collections.sort(datapoints, new Comparator<Datapoint>() {
            @Override
            public int compare(Datapoint t1, Datapoint t2) {
                boolean before = t1.getTimestamp().before(t2.getTimestamp());
                return before ? 1 : -1;
            }
        });

        if(datapoints.size() != 0){
            return datapoints.get(0).getAverage();
        }
        return 0.0;
    }

    private List<Datapoint> calculateCpuUsage(Machine machine){
        Instance instance = machine.getInstance();
        long offsetInMilliseconds = 1000 * 60 * 10;
        Dimension instanceDimension = new Dimension();
        instanceDimension.setName("InstanceId");
        List<Dimension> dims = new ArrayList<Dimension>();
        List<Datapoint> datapoints = new ArrayList<>();
        dims.add(instanceDimension);

        String name = instance.getInstanceId();
        String state = instance.getState().getName();
        if (state.equals("running")) {
            instanceDimension.setValue(name);
            GetMetricStatisticsRequest request = new GetMetricStatisticsRequest()
                    .withStartTime(new Date(new Date().getTime() - offsetInMilliseconds))
                    .withNamespace("AWS/EC2")
                    .withPeriod(60)
                    .withMetricName("CPUUtilization")
                    .withStatistics("Average")
                    .withDimensions(instanceDimension)
                    .withEndTime(new Date());
            GetMetricStatisticsResult getMetricStatisticsResult =
                    cloudWatch.getMetricStatistics(request);
            datapoints = getMetricStatisticsResult.getDatapoints();
        }

        return datapoints;
    }


    synchronized public List<Machine> getRunningMachines() {
        return runningMachines;
    }

    synchronized public List<Machine> getBootingMachines() {
        return runningMachines;
    }

    synchronized public void addRequestToMachine(Machine machine, Request request){
        runningMachines.get(runningMachines.indexOf(machine)).addRequest(request.getId(), request);
        machine.resetNoWorkTimes();
        removeRequest();//If added a new request to maniche maybe it has machines availabe
    }
    synchronized public void launchMachine() {
        if(this.runningMachines.size() >= 10)
            return;
        Machine newMachine = new Machine(startInstance());
        bootingMachines.add(newMachine);
        System.out.println("New Machine in booting process");
    }

    synchronized public void terminateMachine(Machine machine) {
        if(this.runningMachines.size() == 1) {
            machine.resetNoWorkTimes();
            return;
        }
        terminateInstance(machine);
        runningMachines.remove(machine);
    }

    private Instance startInstance(){
        RunInstancesRequest runInstancesRequest = new RunInstancesRequest();

        runInstancesRequest.withImageId("ami-024c8628b700157f4")
                .withInstanceType("t2.micro")
                .withMinCount(1)
                .withMaxCount(1)
                .withKeyName("CNV-lab-AWS")
                .withSecurityGroups("CNV-ssh+http-chckpoint");
        RunInstancesResult runInstancesResult = ec2.runInstances(runInstancesRequest);

        return runInstancesResult.getReservation().getInstances().get(0);
    }

    private void terminateInstance(Machine machine){
        TerminateInstancesRequest termInstanceReq = new TerminateInstancesRequest();
        termInstanceReq.withInstanceIds(machine.getInstance().getInstanceId());
        ec2.terminateInstances(termInstanceReq);
    }
}
