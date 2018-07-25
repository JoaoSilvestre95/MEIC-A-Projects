package webserver;

import java.io.IOException;
import java.io.OutputStream;
import java.io.UnsupportedEncodingException;
import java.net.InetSocketAddress;
import java.net.URLDecoder;
import java.util.HashMap;
import java.util.Map;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import database.MSSManager;

public class WebServer{	

    private static MSSManager mssManager;

    public static void main(String[] args) throws Exception {
        mssManager = new MSSManager();
        mssManager.initDatabase();
        HttpServer server = HttpServer.create(new InetSocketAddress(8001), 0);
        server.createContext("/ping", new MyHandler());
        server.createContext("/mzrun.html", new MazeRequestParser());
        server.setExecutor(java.util.concurrent.Executors.newCachedThreadPool()); // creates a cached ThreadPool
        server.start();
        System.out.println("WebServer Up!");
    }
    
    static class MazeRequestParser implements HttpHandler{
    	@Override
    	public void handle(HttpExchange t) throws IOException{    		
    		
    		String query = t.getRequestURI().getQuery();
    		Map<String, String> parameters = parseHtmlRequest(query);
    		MazeRunnerHandler handler = new MazeRunnerHandler(mssManager,parameters, Thread.currentThread().getId(), t);
    		try {
				handler.runMaze();
			} catch (Exception e){
				e.printStackTrace();
			}
    		/*t.sendResponseHeaders(200, query.length());
            OutputStream os = t.getResponseBody();
            os.write(query.getBytes());
            os.close();*/
    	}
    	
    	private Map<String,String> parseHtmlRequest(String request) throws UnsupportedEncodingException{
    		Map<String, String> maze_parameters = new HashMap<>();
    		
    		String parameters[] = request.split("&");
    		for (String pair : parameters){
    			int idx = pair.indexOf("=");
    	        maze_parameters.put(URLDecoder.decode(pair.substring(0, idx), "UTF-8"), URLDecoder.decode(pair.substring(idx + 1), "UTF-8"));
    		}
    		return maze_parameters;
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
