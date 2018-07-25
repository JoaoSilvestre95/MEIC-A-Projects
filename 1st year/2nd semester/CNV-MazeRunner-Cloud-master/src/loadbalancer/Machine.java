package loadbalancer;

import com.amazonaws.services.ec2.model.Instance;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.HashMap;

public class Machine {


    private Instance instance;
    private String ip;
    private final int port = 8001;
    private double cpu;
    private int noWorkTimes;
    public HashMap<Long, Request> requests;

    public Machine(Instance instance){
        this.ip = instance.getPublicIpAddress();
        this.instance = instance;
        this.cpu = 0.0;
        this.requests = new HashMap<>();
        this.noWorkTimes = 0;
    }

    public Instance getInstance() {
        return instance;
    }

    public void setInstance(Instance instance) {
        this.instance = instance;
        this.ip = instance.getPublicIpAddress();
    }

    public double getCpu() {
        return cpu;
    }

    public void setCpu(double cpu) {
        this.cpu = cpu;
    }

    public void addRequest(Long id, Request request) {
        requests.put(id, request);
    }

    public void removeRequests(Long id) {
        requests.remove(id);
    }

    public String getIp() {
        return ip;
    }

    public long getMachineLoad() {
        long load = 0;
        for (Request request : requests.values()){
            load += request.getWeight();
        }
        return load;
    }

    public int getNoWorkTimes(){
        return this.noWorkTimes;
    }

    public void incNoWorkTimes(){
        this.noWorkTimes++;
    }

    public void resetNoWorkTimes(){
        this.noWorkTimes = 0;
    }

    public boolean ping(){
        String url = "http://" + this.instance.getPublicIpAddress() + ":" + this.port + "/" + "ping";
        System.out.println(url);
        StringBuilder response = new StringBuilder();
        URL urlObj;
        try {
            urlObj = new URL (url);
            HttpURLConnection connection = (HttpURLConnection) urlObj.openConnection();
            connection.setDoOutput(true);
            connection.setRequestMethod("GET");
            connection.setConnectTimeout(10000);
            connection.setReadTimeout(10000);
            BufferedReader rd = new BufferedReader(new InputStreamReader(connection.getInputStream()));
            String line;
            while ((line = rd.readLine()) != null) {
                response.append(line);
            }
            rd.close();
            System.out.println("With ping request machine: " + this.instance.getInstanceId() + " responded: " + response.toString());

        } catch (MalformedURLException e) {
            System.out.println("Malformed URL");
            return false;
        } catch (IOException e) {
            System.out.println("Problem connecting to instance");
            return false;
        }
        return true;
    }
}
