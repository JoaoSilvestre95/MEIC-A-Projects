package webserver;

import java.io.IOException;
import java.io.OutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Map;

import com.sun.net.httpserver.HttpExchange;

import database.MSSManager;
import pt.ulisboa.tecnico.meic.cnv.mazerunner.maze.Main;
import pt.ulisboa.tecnico.meic.cnv.mazerunner.maze.exceptions.CantGenerateOutputFileException;
import pt.ulisboa.tecnico.meic.cnv.mazerunner.maze.exceptions.CantReadMazeInputFileException;
import pt.ulisboa.tecnico.meic.cnv.mazerunner.maze.exceptions.InvalidCoordinatesException;
import pt.ulisboa.tecnico.meic.cnv.mazerunner.maze.exceptions.InvalidMazeRunningStrategyException;
import tool.OurTool;

public class MazeRunnerHandler {
	private String mazeIn;
	private String mazeOut;
	private String xStart;
	private String yStart;
	private String xFinal;
	private String yFinal;
	private String velocity;
	private String strategy;
	private HttpExchange t;
	private MSSManager mssManager;
	private String mazeName;

	public MazeRunnerHandler(MSSManager manager, Map<String,String> parameters, long threadId, HttpExchange t){
		this.mazeName = parameters.get("m");
		this.mazeIn = "src/mazerunner/" + this.mazeName;
		this.mazeOut = "src/mazerunner/" + this.mazeName + threadId + ".html";
		this.xStart = parameters.get("x0");
		this.yStart = parameters.get("y0");
		this.xFinal = parameters.get("x1");
		this.yFinal = parameters.get("y1");
		this.velocity = parameters.get("v");
		this.strategy = parameters.get("s");
		this.t = t;
		this.mssManager = manager;
	}

	public void runMaze() throws CantReadMazeInputFileException, InvalidMazeRunningStrategyException, CantGenerateOutputFileException, InvalidCoordinatesException, IOException{
		String[] args = {this.xStart, this.yStart, this.xFinal, this.yFinal, this.velocity, this.strategy, this.mazeIn, this.mazeOut};
		long start = System.currentTimeMillis();
		Main.main(args);
		//writeToFile();
		long elapsedTime = System.currentTimeMillis() - start;
		System.out.println(elapsedTime/1000F);

		mssManager.addItem(mazeName,strategy,velocity,OurTool.getBBCounter());

		OurTool.removeBBCounter();

		Path path = Paths.get(this.mazeOut);
		byte []img;
		if (Files.exists(path)){
			img = Files.readAllBytes(path);
	        try {
			    t.sendResponseHeaders(200, img.length);
			    OutputStream os = t.getResponseBody();
			    os.write(img);
			    os.close();
			} catch (IOException e) {
				e.printStackTrace();
			}
		}
	}

	public void writeToFile(){
		File file = new File("extracted_metrics.txt");
		long bbCount = OurTool.getBBCounter();
		String id = "maze: " + mazeIn + " x0: " + xStart + " y0: " + yStart + " x1: " + xFinal + " y1: " + yFinal + " velocity: " + velocity + " strategy: " + strategy + " metric: " + bbCount + "\n";
		byte[] header = id.getBytes();

		try {
			if(!file.exists()){
					file.createNewFile();
			}
			FileOutputStream outputStream = new FileOutputStream(file, true);
			outputStream.write(header);
			outputStream.close();

		} catch(IOException e){
			e.printStackTrace();
		}
	}
}
