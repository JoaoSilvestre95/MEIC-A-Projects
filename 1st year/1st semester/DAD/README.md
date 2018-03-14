<br /># DAD's Online Gaming Platform (DAD-OGP)
The goal of this project is to design and implement DAD-OGP, a fault-tolerant on-line gaming platform. The focus of the project is on the design and evaluation of mechanisms aimed at ensuring diﬀerent consistency levels in presence of crash faults, rather than on the integration of advanced graphics libraries or the implementation of complex game logics (e.g., AI). In the light of the above observations, for simplicity, the DAD-OGP platform will be composed of two main components:<br />
  * a multi-player version of the PacMan game.<br />
  * a chat allowing communication among players involved in a game.
  
From an architectural perspective, the DAD-OGP platform is composed of two main modules: <br />
  * The Client module: a Windows form application that provides end users with access to both the gaming zone and the chat via a uniﬁed User Interface (UI).<br /> 
  * The Server module: the component responsible for maintaining the state of the online game and for synchronize the actions submitted in real-time by players. Students are free to opt for implementing the server side via either a Console application or a Windows form application.<br />

The project shall be implemented using C# and .Net Remoting using Microsoft Visual Studio and the CLR runtime.


Commands to test(localhost): <br /> 
StartServer SERVER tcp://localhost:11000/PCS tcp://localhost:8086/PacmanServer 50 6<br /> 
StartClient CLIENT tcp://localhost:11000/PCS tcp://localhost:8087/random 50 6<br /> 
StartClient CLIENT1 tcp://localhost:11000/PCS tcp://localhost:8088/random 50 6<br /> 
StartClient CLIENT2 tcp://localhost:11000/PCS tcp://localhost:8089/random 50 6<br /> 
StartClient CLIENT3 tcp://localhost:11000/PCS tcp://localhost:8090/random 50 6<br />
StartClient CLIENT4 tcp://localhost:11000/PCS tcp://localhost:8091/random 50 6<br />
StartClient CLIENT5 tcp://localhost:11000/PCS tcp://localhost:8092/random 50 6<br /> 


How to Start a Project Manually:  <br /> 
  * Launch 1 PupperMaster<br /> 
  * Launch 1 PCS by Computer<br /> 
  * Execute known commands on PupperMaster<br /> 
  
From Script: <br /> 
ReadFromScript [filelocation]<br /> 
