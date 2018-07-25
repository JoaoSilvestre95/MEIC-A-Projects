package tool;

import BIT.highBIT.*;
import java.io.*;
import java.util.*;

public class OurTool {

    private static HashMap<Long, Long> metrics = new HashMap<>();

    private static String[] BLACK_LIST = {"exceptions", "render"};

    public static void main(String argv[]) {
        File file_in = new File(argv[0]);
        instrumentFiles(file_in.listFiles());
    }

    public static synchronized void incrementBBCounter(int incr){
      long threadId = Thread.currentThread().getId();
      if(metrics.containsKey(threadId)){
        metrics.put(threadId, metrics.get(threadId) + (long) incr);
      }
      else{
        metrics.put(threadId, (long) incr);
      }
    }

    public static long getBBCounter() {
        return metrics.get(Thread.currentThread().getId());
    }

    public static void removeBBCounter() {
        metrics.remove(Thread.currentThread().getId());
    }

    public static void instrumentFiles(File[] files) {
        for (File file : files) {
            if (file.isDirectory()) {
                if(containsName(file.getName())) continue;
                instrumentFiles(file.listFiles());
            } else {
                if (file.getName().endsWith(".class")){
                    instrumentFile(file);
                }
            }
        }
    }

    public static void instrumentFile(File file){
        System.out.println("Instrument file: " + file.getName());
        ClassInfo ci = new ClassInfo(file.getPath());
        Vector routines = ci.getRoutines();

        for (Enumeration e = routines.elements(); e.hasMoreElements(); ) {
          Routine routine = (Routine) e.nextElement();
          // BasicBlockArray bba = routine.getBasicBlocks();
          //
          // routine.addBefore("tool/OurTool", "incrementBBCounter", new Integer(bba.size()));
          for (Enumeration b = routine.getBasicBlocks().elements(); b.hasMoreElements(); ) {
             BasicBlock bb = (BasicBlock) b.nextElement();
             bb.addBefore("tool/OurTool", "incrementBBCounter", new Integer(1));
          }
        }
        ci.write(file.getPath());
    }

    public static boolean containsName(String name){
        for(String s: BLACK_LIST){
            if(s.equals(name)) return true;
        }
        return false;
    }
}
