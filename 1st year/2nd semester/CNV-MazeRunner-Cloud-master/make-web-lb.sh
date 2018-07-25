#!/usr/bin/env bash

export CLASSPATH=lib/aws-java-sdk-1.11.333/lib/aws-java-sdk-1.11.333.jar:lib/aws-java-sdk-1.11.333/third-party/lib/*:bin/:.

lb_path="src/loadbalancer"
db_path="src/database"

javac -d bin $lb_path/*.java
javac -d bin $db_path/MSSManager.java
java loadbalancer.WebServer
