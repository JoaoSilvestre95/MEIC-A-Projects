package loadbalancer;

import java.math.BigInteger;

public class Request {
    private long id;
    private String scheme;
    private String context;
    private String query;
    private long weight;

    public Request(long id, String scheme, String context, String query, long weight){
        this.id = id;
        this.scheme = scheme;
        this.context = context;
        this.query = query;
        this.weight = weight;
    }

    public long getId() {
        return id;
    }

    public String getScheme() {
        return this.scheme;
    }

    public String getContext() {
        return this.context;
    }

    public String getQuery() {
        return this.query;
    }

    public Long getWeight() {
        return this.weight;
    }

    synchronized public void waitForMachine(){
        try {
            this.wait();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }

    synchronized public void wake(){
        try {
            this.notify();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
