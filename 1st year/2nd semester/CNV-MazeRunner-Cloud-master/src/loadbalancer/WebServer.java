package loadbalancer;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import database.MSSManager;

import java.io.IOException;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.net.InetSocketAddress;
import java.net.URLDecoder;
import java.util.HashMap;
import java.util.Map;

public class WebServer{

    private static LoadBalancer loadBalancer;

    public static void main(String[] args) throws Exception {
        loadBalancer = new LoadBalancer(new MSSManager());

        HttpServer server = HttpServer.create(new InetSocketAddress(8000), 0);
        server.createContext("/test", new MyHandler());
        server.createContext("/mzrun.html", new MazeRequestHandler());
        server.setExecutor(java.util.concurrent.Executors.newCachedThreadPool()); // creates a cached ThreadPool
        server.start();
        System.out.println("WebServer LB Up!");
    }

    static class MazeRequestHandler implements HttpHandler{
        @Override
        public void handle(HttpExchange t) throws IOException {

            long id = Thread.currentThread().getId();
            String scheme = /*t.getRequestURI().getScheme()*/"http";
            String context = t.getRequestURI().getPath();
            String query = t.getRequestURI().getQuery();
            long weight = loadBalancer.calculateWeight(query);
            Request request = new Request(id, scheme, context, query, weight);
            byte[] response = loadBalancer.route(request);


            try {
                t.sendResponseHeaders(200, 0); //TODO Whats the length?
                OutputStream os = t.getResponseBody();
                os.write(response);
                os.close();
            } catch (IOException e) {
                e.printStackTrace();
            }

        }
    }

    static class MyHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange t) throws IOException {
            String query = t.getRequestURI().getQuery() + " Thread Id: " + Thread.currentThread().getId();
            t.sendResponseHeaders(200, query.length());
            OutputStream os = t.getResponseBody();
            os.write(query.getBytes());
            os.close();
        }
    }


}
