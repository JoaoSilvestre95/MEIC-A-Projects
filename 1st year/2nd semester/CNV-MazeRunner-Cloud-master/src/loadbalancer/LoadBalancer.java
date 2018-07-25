package loadbalancer;

import java.io.*;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLDecoder;
import java.util.*;

import com.amazonaws.services.dynamodbv2.document.Item;
import com.amazonaws.services.dynamodbv2.model.AttributeValue;
import com.amazonaws.services.dynamodbv2.xspec.S;
import database.MSSManager;

public class LoadBalancer {

    private AutoScaler autoScaler;
    public final static long MAXLOAD = 15877328177L;
    private MSSManager mssManager;

    //private MSSManager MSSManager;

    public LoadBalancer(MSSManager manager) throws InterruptedException {
        autoScaler = new AutoScaler();
        mssManager = manager;
        mssManager.initDatabase();
    }

    public byte[] route(Request request) {
        System.out.println("New request incoming! " + request.getId());
        byte[] response = null;
        Machine machine = selectLeastLoadedMachine(request.getWeight());

        if (machine != null) {
            autoScaler.addRequestToMachine(machine, request);
            response = runRequest(machine, request);
            autoScaler.removeRequest();
            return response;
        } else {
            System.out.println("Have to wait for new machine! " + request.getId());
            autoScaler.requestMachines(request);
            request.waitForMachine();
            System.out.println("Trying again request! " + request.getId());
            return route(request);

        }
    }

    synchronized private Machine selectLeastLoadedMachine(long load){
        List<Machine> machines = autoScaler.getRunningMachines();
        if(machines.size() == 0) return null;
        Machine lessLoadedMachine = machines.get(0);
        for (Machine m : machines){
            if(m.getMachineLoad() < lessLoadedMachine.getMachineLoad())
                lessLoadedMachine = m;
        }
        if(lessLoadedMachine.getMachineLoad() + load < (MAXLOAD + 1))
            return lessLoadedMachine;
        else
            return null;
    }

    public byte[] runRequest(Machine machine, Request request){
        String url = request.getScheme() + "://"
                + machine.getIp()
                + ":" + 8001
                + request.getContext() + "?" + request.getQuery();
        System.out.print("Request with id: " + request.getId() + "gonna redirect to: " + url);
        byte[] response;
        URL urlObj;

        try {
            urlObj = new URL(url);
            HttpURLConnection connection = (HttpURLConnection) urlObj.openConnection();
            connection.setRequestMethod("GET");
            connection.setDoOutput(true);
            int responseCode = connection.getResponseCode();
            System.out.println("\nSending 'GET' request to URL : " + url);
            System.out.println("Response Code : " + responseCode);

            byte[] buffer = new byte[8192]; //TODO why? how this works?
            int byteRead;
            ByteArrayOutputStream output = new ByteArrayOutputStream();

            InputStream is = connection.getInputStream();

            while((byteRead = is.read(buffer)) != -1){
                output.write(buffer, 0, byteRead);
            }
            response = output.toByteArray();
            output.close();
            machine.removeRequests(request.getId());
            return response;
        } catch (IOException e) {
            e.printStackTrace();
        }
        return null;
    }

    private Map<String,String> parseQuery(String query) throws UnsupportedEncodingException{
        Map<String, String> maze_parameters = new HashMap<>();

        String parameters[] = query.split("&");
        for (String pair : parameters){
            int idx = pair.indexOf("=");
            maze_parameters.put(URLDecoder.decode(pair.substring(0, idx), "UTF-8"), URLDecoder.decode(pair.substring(idx + 1), "UTF-8"));
        }

        return maze_parameters;
    }

    /*private long bestWeightApproximation(/*Map<String, String> attributesString mazeSize, String strategy, String velocity) {
        long weight = DEFAULT_WEIGHT;

        List<Map<String, AttributeValue>> match = mssManager.getItemsWithCondition("mazeSize",mazeSize);

        if(match.size() != 0){

        }

        for(String attrName: attributes.keySet()) {
            for(Map<String, AttributeValue> match: mssManager.getItemsWithCondition(attrName,attributes.get(attrName))){

            }
        }

        return weight;
    }*/

    synchronized public long calculateWeight(String query) {
        Map<String,String> parameters = new HashMap<>();
        long weight = MAXLOAD;

        try {
            parameters = parseQuery(query);
        } catch (UnsupportedEncodingException e) {
            e.printStackTrace();
        }

        String mazeSize = parameters.get("m");
     /* Integer xStart =  Integer.parseInt(parameters.get("x0"));
        Integer yStart =  Integer.parseInt(parameters.get("y0"));
        Integer xFinal =  Integer.parseInt(parameters.get("x1"));
        Integer yFinal =  Integer.parseInt(parameters.get("y1"));*/
        String velocity = parameters.get("v");
        String strategy = parameters.get("s");

        int pk = mssManager.getHashCode(mazeSize,strategy,velocity);
        Item match = mssManager.getItemByPK(pk);

        System.out.println("Request hashcode: " + pk);

        if(match != null) {
            weight = match.getLong("metric");
        }

        System.out.println("Calculated metric: " + weight);

        return weight;
    }
}
