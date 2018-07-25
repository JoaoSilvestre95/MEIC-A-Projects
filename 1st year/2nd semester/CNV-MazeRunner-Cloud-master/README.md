# CNV-MazeRunner-Cloud
The purpose of this project is to enable you to gain experience in cloud computing using  Amazon Web Services ecosystem. <br />
System Requirements: <br />
java 1.7 <br /><br />

On root: <br />
* Has src folder, lib and scripts to launch the webservers

On source: <br />
* Package loadbalancer: <br />
   Has a webserver, a loadbalancer that comunicate with autoscaler <br/>
   handles requests and passes them to the autoscaler if needed. Auto scaler has machines with requests
* Package database: <br />
    Contains the manager to metrics storage system
* Package Tool: <br />
    Contains our instrumation tool that counts basic blocks
* Package Mazerunner: <br />
    Contains mazerunner classes
<br />


How to run: <br />
At root/ <br />
$ source ./java-config-alameda.sh <br />
$ source ./make-web-maze.sh <br />
$ source ./make-web-lb.sh <br />
Request examples:
<br />
http://localhost:8000/mzrun.html?m=Maze100.maze&x0=3&y0=9&x1=78&y1=89&v=50&s=astar

http://localhost:8000/mzrun.html?m=Maze300.maze&x0=6&y0=24&x1=156&y1=245&v=50&s=astar
