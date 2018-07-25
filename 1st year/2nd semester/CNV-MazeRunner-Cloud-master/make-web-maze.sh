#!/usr/bin/env bash
source java-config-alameda.sh

#export CLASSPATH=/home/ec2-user/CNV-MazeRunner-Cloud/lib/aws-java-sdk-1.11.333/lib/aws-java-sdk-1.11.333.jar:/home/ec2-user/CNV-MazeRunner-Cloud/lib/aws-java-sdk-1.11.333/third-party/lib/*:bin/:.

export CLASSPATH=~/Documents/CNV/CNV-MazeRunner-Cloud/lib/aws-java-sdk-1.11.333/lib/aws-java-sdk-1.11.333.jar:~/Documents/CNV/CNV-MazeRunner-Cloud/lib/aws-java-sdk-1.11.333/third-party/lib/*:bin/:.

maze_path="src/mazerunner/src/main/java/pt/ulisboa/tecnico/meic/cnv/mazerunner/maze"
web_path="src/webserver"
instr_path="src/tool"
db_path="src/database"

rm -rf bin/*
rm -f extracted_metrics.txt

javac -d bin lib/BIT/highBIT/*.java lib/BIT/lowBIT/*.java
javac -d bin $db_path/MSSManager.java
javac -d bin $maze_path/exceptions/*.java $maze_path/render/*.java $maze_path/strategies/datastructure/*.java $maze_path/strategies/*.java $maze_path/*.java
javac -d bin $instr_path/*.java
javac -d bin $web_path/*.java

java tool.OurTool bin/pt/
java webserver.WebServer
