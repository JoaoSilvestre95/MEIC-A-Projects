using GameApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoundState;
using System.Threading;

namespace PacmanGame
{
    public class Game
    {
        int boardRight = 323;
        int boardBottom = 320;
        int boardLeft = -2;
        int boardTop = 40;
        //player speed
        int speed = 5;

        int total_coins = 61;

        //ghost speed for the one direction ghosts
        int ghost1 = 5;
        int ghost2 = 5;

        //x and y directions for the bi-direccional pink ghost
        int ghost3x = 5;
        int ghost3y = 5;

        //int round = 1;

        Dictionary<int, Rectangle> coins = new Dictionary<int, Rectangle>();
        List<Rectangle> walls;
        Dictionary<string, Pacman> pacmans = new Dictionary<string, Pacman>();

        Rectangle redGhost;
        Rectangle yellowGhost;
        Rectangle pinkGhost;

        public Game(GameState gs)
        {
            Parallel.Invoke(
                () => coins = CreateCoinsFromUpdate(gs),
                () => walls = CreateWalls(),
                () => CreatePacmanFromUpdate(gs),
                () => redGhost = new Rectangle(gs.getXRedGhost(), gs.getYRedGhost(), 30, 30),
                () => yellowGhost = new Rectangle(gs.getXYellowGhost(), gs.getYYellowGhost(), 30, 30),
                () => pinkGhost = new Rectangle(gs.getXPinkGhost(), gs.getYPinkGhost(), 30, 30),
                () => ghost3x = gs.getPinkX(),
                () => ghost3y = gs.getPinkY(),
                () => ghost2 = gs.getYellow(),
                () => ghost1 = gs.getRed()
            );
            updateDirection(gs);
            Console.WriteLine("Game State Recovered");
        }

        public Game(List<string> pids)
        {
            int i = 1;
            Parallel.Invoke(
                () => redGhost = new Rectangle(180, 73, 30, 30),
                () => yellowGhost = new Rectangle(221, 273, 30, 30),
                () => pinkGhost = new Rectangle(301, 72, 30, 30),
                () => coins = CreateCoins(),
                () => walls = CreateWalls()
            );
            Console.WriteLine("Players:");
            foreach (string pid in pids)
            {
                CreatePacman(pid, i);
                i++;
                Console.WriteLine(pid);
            }
            Console.WriteLine();
        }

        private Dictionary<int, Rectangle> CreateCoinsFromUpdate(GameState gs)
        {
            Dictionary<int, Rectangle> coinsa = new Dictionary<int, Rectangle>();
            for (int i = 0; i< gs.getCountCoins(); i++)
            {
                int id = gs.getCoinID(i);
                coinsa.Add(id, new Rectangle(gs.getXCoin(id), gs.getYCoin(id), 25, 25));
            }
            return coinsa;
        }

        private void CreatePacmanFromUpdate(GameState gs)
        {
            for (int i = 0; i < gs.getCountPacmans(); i++)
            {
                String pid = gs.getPacmanPID(i);
                pacmans.Add(pid, new Pacman(pid, gs.getXPacman(pid), gs.getYPacman(pid), gs.getScore(pid), gs.getState(pid)));
            }
        }

        private void CreatePacman(string pid, int numberOfPlayer)
        {
            pacmans.Add(pid, new Pacman(pid, numberOfPlayer));
        }

        private Dictionary<int, Rectangle> CreateCoins() 
        {
            Dictionary<int, Rectangle> coinsa = new Dictionary<int, Rectangle>();
            int i = 1;
            for (int x = 8; x <= 328; x += 40)
            {
                for (int y = 40; y <= 320; y += 40)
                {
                    if (((x == 88 || x == 248) && y < 160)) continue; // WALLS
                    if (((x == 128 || x == 288) && y > 200)) continue;
                    coinsa.Add(i, new Rectangle(x, y, 25, 25));
                    i++;
                }
            }
            return coinsa;
        }

        private List<Rectangle> CreateWalls()
        {
            List<Rectangle> walls = new List<Rectangle>
            {
                new Rectangle(88, 40, 15, 95),
                new Rectangle(248, 40, 15, 95),
                new Rectangle(128, 240, 15, 95),
                new Rectangle(288, 240, 15, 95)
            };
            return walls;
        }

        public GameState GetState(int round)
        {
            GameState gstate = new GameState(round, ghost3x, ghost3y, ghost1, ghost2);

            gstate.InsertRedGhost(redGhost.X, redGhost.Y);
            gstate.InsertPinkGhost(pinkGhost.X, pinkGhost.Y);
            gstate.InsertYellowGhost(yellowGhost.X, yellowGhost.Y);

            foreach (var x in pacmans)
            {
                gstate.InsertPacman(x.Value.getPID(), x.Value.getX(), x.Value.getY(), x.Value.getDirection(), x.Value.getState(), x.Value.getScore());
            }

            foreach (var y in coins)
            {
                gstate.SetVisibleCoin(y.Key);
                gstate.setXCoin(y.Key, y.Value.X);
                gstate.setYCoin(y.Key, y.Value.Y);
            }

            foreach (var z in walls)
            {
                gstate.InsertWall(z.X, z.Y);
            }

            return gstate;
        }

        public void GameLoop(Dictionary<string, string> remoteQueue)
        {
            Parallel.Invoke(() => {if (remoteQueue.Count > 0)
                                    CalculatePositionPacman(remoteQueue);}, 
                            () => CalculatePositionGhosts());

            Parallel.Invoke(() => CheckGameOver(), 
                            () => CollectCoin());
        }

        public void updateDirection()
        {
            foreach (KeyValuePair<string,Pacman> z in pacmans)
            {
                switch (z.Value.getDirection())
                {
                    case 0:
                        z.Value.setDirection(4);
                        break;
                    case 1:
                        z.Value.setDirection(5);
                        break;
                    case 2:
                        z.Value.setDirection(6);
                        break;
                    case 3:
                        z.Value.setDirection(7);
                        break;
                }
            }
        }

        public void updateDirection(GameState gs)
        {
            Parallel.ForEach(pacmans.ToList(), (z) =>
            {
                switch (gs.getDirectionPacman(z.Key))
                {
                    case 0:
                        z.Value.setDirection(0);
                        break;
                    case 1:
                        z.Value.setDirection(1);
                        break;
                    case 2:
                        z.Value.setDirection(2);
                        break;
                    case 3:
                        z.Value.setDirection(3);
                        break;
                    case 4:
                        z.Value.setDirection(0);
                        break;
                    case 5:
                        z.Value.setDirection(1);
                        break;
                    case 6:
                        z.Value.setDirection(2);
                        break;
                    case 7:
                        z.Value.setDirection(3);
                        break;
                }
            });
        }

        private void CollectCoin()
        {
            foreach (var pacman in pacmans)
            {
                foreach (var x in coins)
                {
                    if (x.Value.IntersectsWith(pacman.Value.getRectangle()))
                    {
                        coins.Remove(x.Key);
                        pacman.Value.increaseScore();
                        if (coins.Count == 0)
                        {
                            decideWinner();
                        }
                        if (pacman.Value.getScore() == total_coins)
                        {
                            pacman.Value.setX(0);
                            pacman.Value.setY(25);
                            pacman.Value.setState(1);
                        }
                        break;
                    }
                }
            }
        }

        private void decideWinner()
        {            
            Pacman pac = null;
            int biggest = 0;
            foreach (var pacman in pacmans)
            {
                if (pacman.Value.getScore() > biggest && pacman.Value.getState() != -1)
                {
                    biggest = pacman.Value.getScore();
                    pac = pacman.Value;
                }
                pacman.Value.setX(0);
                pacman.Value.setY(25);
                pacman.Value.setState(-1);
            }
            pac.setState(1);
        }

        private void CalculatePositionPacman(Dictionary<string, string> remoteQueue)
        {
            void calcPacman(KeyValuePair<string, string> moviment)
            {
                if (pacmans.ContainsKey(moviment.Key) && pacmans[moviment.Key].getState() != -1)
                {
                    //move player
                    if (moviment.Value.Equals("LEFT"))
                    {
                        if (pacmans[moviment.Key].getX() - speed >= boardLeft)
                            pacmans[moviment.Key].increaseX(-speed);

                        switch (pacmans[moviment.Key].getDirection())
                        {
                            case 4:
                                break;
                            default:
                                pacmans[moviment.Key].setDirection(0);
                                break;
                        }
                    }
                    else if (moviment.Value.Equals("RIGHT"))
                    {
                        if (pacmans[moviment.Key].getX() + speed <= boardRight)
                            pacmans[moviment.Key].increaseX(speed);

                        switch (pacmans[moviment.Key].getDirection())
                        {
                            case 5:
                                break;
                            default:
                                pacmans[moviment.Key].setDirection(1);
                                break;
                        }
                    }
                    else if (moviment.Value.Equals("UP"))
                    {
                        if (pacmans[moviment.Key].getY() - speed >= boardTop)
                            pacmans[moviment.Key].increaseY(-speed);

                        switch (pacmans[moviment.Key].getDirection())
                        {
                            case 6:
                                break;
                            default:
                                pacmans[moviment.Key].setDirection(2);
                                break;
                        }
                    }
                    else if (moviment.Value.Equals("DOWN"))
                    {
                        if (pacmans[moviment.Key].getY() + speed <= boardBottom)
                            pacmans[moviment.Key].increaseY(speed);

                        switch (pacmans[moviment.Key].getDirection())
                        {
                            case 7:
                                break;
                            default:
                                pacmans[moviment.Key].setDirection(3);
                                break;
                        }
                    }
                }

            }
            Parallel.ForEach(remoteQueue.ToList(), (moviment) => {
                    calcPacman(moviment);
            });
        }

        private void CalculatePositionGhosts()
        {
            //move ghosts
            redGhost.X += ghost1;
            yellowGhost.X += ghost2;

            // if the red ghost hits the picture box 4 then wereverse the speed
            if (redGhost.IntersectsWith(walls[0]))
                ghost1 = -ghost1;
            // if the red ghost hits the picture box 3 we reverse the speed
            else if (redGhost.IntersectsWith(walls[1]))
                ghost1 = -ghost1;
            // if the yellow ghost hits the picture box 1 then wereverse the speed
            if (yellowGhost.IntersectsWith(walls[2]))
                ghost2 = -ghost2;
            // if the yellow chost hits the picture box 2 then wereverse the speed
            else if (yellowGhost.IntersectsWith(walls[3]))
                ghost2 = -ghost2;
            //moving ghosts and bumping with the walls end
            //for loop to check walls, ghosts and points

            pinkGhost.X += ghost3x;
            pinkGhost.Y += ghost3y;

            if (pinkGhost.Left < boardLeft ||
                pinkGhost.Left > boardRight ||
                (pinkGhost.IntersectsWith(walls[0])) ||
                (pinkGhost.IntersectsWith(walls[1])) ||
                (pinkGhost.IntersectsWith(walls[2])) ||
                (pinkGhost.IntersectsWith(walls[3])))
            {
                ghost3x = -ghost3x;
            }

            if (pinkGhost.Top < boardTop || pinkGhost.Top + pinkGhost.Height > boardBottom - 2)
            {
                ghost3y = -ghost3y;
            }
        }

        private void CheckGameOver()
        {
            Parallel.ForEach(pacmans.ToList(), (pacman) => {
                foreach (Rectangle wall in walls)
                {
                    Rectangle pacmanZ = pacman.Value.getRectangle();
                    if (wall.IntersectsWith(pacmanZ) || pinkGhost.IntersectsWith(pacmanZ) || yellowGhost.IntersectsWith(pacmanZ) || redGhost.IntersectsWith(pacmanZ))
                    {
                        pacman.Value.setX(0);
                        pacman.Value.setY(25);
                        pacman.Value.setState(-1);
                    }
                }
            });
        }
    }

    public class Pacman
    {
        private int score;
        private Rectangle position;
        private string pid;
        private int state;
        private int direction;

        public Pacman(string pid, int numberOfPlayer)
        {
            this.pid = pid;
            score = 0;
            position =  new Rectangle(8, numberOfPlayer * 40, 25, 25);
            state = 0;
        }

        public Pacman(string pid, int x, int y, int score, int state)
        {
            this.pid = pid;
            this.score = score;
            position = new Rectangle(x, y, 25, 25);
            this.state = state;
        }

        public string getPID()
        {
            return pid;
        }
        public int getState()
        {
            return state;
        }
        public int getScore()
        {
            return score;
        }
        public void setX(int x)
        {
            position.X = x;
        }
        public void setY(int y)
        {
            position.Y = y;
        }
        public void setState(int state)
        {
            this.state = state;
        }
        public int getX()
        {
            return position.X;
        }
        public int getY()
        {
            return position.Y;
        }
        public Rectangle getRectangle()
        {
            return position;
        }

        public void increaseScore()
        {
            score++;
        }
        public void increaseX(int inc)
        {
            position.X += inc;
        }
        public void increaseY(int inc)
        {
            position.Y += inc;
        }

        public void setRectangle(Rectangle newPosition)
        {
            this.position = newPosition;
        }

        public void setDirection(int dir)
        {
            this.direction = dir;
        }
        public int getDirection()
        {
            return direction;
        }
    }
}

