using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoundState
{
    [Serializable]
    public class GameState
    {
        private int round;
        private List<MovableGameObject> Ghosts = new List<MovableGameObject>();
        private List<MovableGameObject> Pacmans = new List<MovableGameObject>();
        private List<Wall> Walls = new List<Wall>();
        private List<Coin> Coins = new List<Coin>();
        private Tuple<int, int> pinkghost;
        private int redGhost;
        private int yellowGhost;

        public GameState(int round, int pinkX, int pinkY, int red, int yellow)
        {
            this.round = round;
            for (int x = 1; x <= 60; x++)
            {
                Coins.Add(new Coin(x, false));
            }
            this.pinkghost = new Tuple<int, int>(pinkX,pinkY);
            this.redGhost = red;
            this.yellowGhost = yellow;
        }

        public int getState(string PID)
        {
            foreach (MovableGameObject pac in Pacmans)
            {
                if (pac.getPID().Equals(PID))
                {
                    return pac.getState();
                }
            }
            return 0;
        }

        public int getPinkX()
        {
            return pinkghost.Item1;
        }

        public int getPinkY()
        {
            return pinkghost.Item2;
        }

        public int getRed()
        {
            return redGhost;
        }

        public int getYellow()
        {
            return yellowGhost;
        }

        public int getRound()
        {
            return round;
        }

        public int getScore(string PID)
        {
            foreach (MovableGameObject x in Pacmans)
            {
                if (x.getPID().Equals(PID))
                {
                    return x.getScore();
                }
            }
            return 0;
        }

        public int getXRedGhost()
        {
            foreach (MovableGameObject x in Ghosts)
            {
                if (x.getPID().Equals("RedGhost"))
                {
                    return x.getX();
                }
            }
            return 0;
        }

        public int getYRedGhost()
        {
            foreach (MovableGameObject x in Ghosts)
            {
                if (x.getPID().Equals("RedGhost"))
                {
                    return x.getY();
                }
            }
            return 0;
        }
        public int getXYellowGhost()
        {
            foreach (MovableGameObject x in Ghosts)
            {
                if (x.getPID().Equals("YellowGhost"))
                {
                    return x.getX();
                }
            }
            return 0;
        }

        public int getYYellowGhost()
        {
            foreach (MovableGameObject x in Ghosts)
            {
                if (x.getPID().Equals("YellowGhost"))
                {
                    return x.getY();
                }
            }
            return 0;
        }
        public int getXPinkGhost()
        {
            foreach (MovableGameObject x in Ghosts)
            {
                if (x.getPID().Equals("PinkGhost"))
                {
                    return x.getX();
                }
            }
            return 0;
        }

        public int getYPinkGhost()
        {
            foreach (MovableGameObject x in Ghosts)
            {
                if (x.getPID().Equals("PinkGhost"))
                {
                    return x.getY();
                }
            }
            return 0;
        }

        public void InsertRedGhost(int x, int y)
        {
            Ghosts.Add(new MovableGameObject("RedGhost", x, y));
        }
        public void InsertPinkGhost(int x, int y)
        {
            Ghosts.Add(new MovableGameObject("PinkGhost", x, y));
        }
        public void InsertYellowGhost(int x, int y)
        {
            Ghosts.Add(new MovableGameObject("YellowGhost", x, y));
        }

        public void InsertPacman(string PID, int x, int y, int direction, int state, int score)
        {
            Pacmans.Add(new MovableGameObject(PID, x, y, direction, state, score));
        }

        public void InsertWall(int x, int y)
        {
            Walls.Add(new Wall(x, y));
        }

        public int getXPacman(string PID)
        {
            foreach (MovableGameObject x in Pacmans)
            {
                if (x.getPID().Equals(PID))
                {
                    return x.getX();
                }
            }
            return 0;
        }

        public int getYPacman(string PID)
        {
            foreach (MovableGameObject x in Pacmans)
            {
                if (x.getPID().Equals(PID))
                {
                    return x.getY();
                }
            }
            return 0;
        }

        public int getDirectionPacman(string PID)
        {
            foreach (MovableGameObject x in Pacmans)
            {
                if (x.getPID().Equals(PID))
                {
                    return x.getDirection();
                }
            }
            return 0;
        }

        public int getXWall(int i)
        {
            return Walls[i].getX();
        }

        public int getYWall(int i)
        {
            return Walls[i].getY();
        }

        public int getCountPacmans()
        {
            return Pacmans.Count;
        }

        public int getCountCoins()
        {
            return Coins.Count;
        }

        public int getCountWalls()
        {
            return Walls.Count;
        }
        public void SetVisibleCoin(int id)
        {
            foreach (Coin x in Coins)
            {
                if (x.getID() == id)
                {
                    x.setVisible(true);
                    return;
                }
            }
        }

        public bool getVisibleCoin(int id)
        {
            foreach (Coin x in Coins)
            {
                if (x.getID() == id)
                {
                    return x.getVisible();
                }
            }
            return false;
        }

        public int getXCoin(int id)
        {
            foreach (Coin x in Coins)
            {
                if (x.getID() == id)
                {
                    return x.getX();
                }
            }
            return 0;
        }

        public int getYCoin(int id)
        {
            foreach (Coin x in Coins)
            {
                if (x.getID() == id)
                {
                    return x.getY();
                }
            }
            return 0;
        }

        public void setXCoin(int id, int x)
        {
            foreach (Coin coin in Coins)
            {
                if (coin.getID() == id)
                {
                    coin.setX(x);
                    return;
                }
            }
        }

        public void setYCoin(int id, int y)
        {
            foreach (Coin coin in Coins)
            {
                if (coin.getID() == id)
                {
                    coin.setY(y);
                    return;
                }
            }
        }

        public int getCoinID(int i)
        {
            return Coins[i].getID();
        }

        public String getPacmanPID(int i)
        {
            return Pacmans[i].getPID();
        }

        public int getMovableGameObjectDirection(string PID)
        {
            foreach (MovableGameObject x in Pacmans)
            {
                if (x.getPID().Equals(PID))
                {
                    return x.getDirection();
                }
            }
            return 0;
        }
    }
    [Serializable]
    class MovableGameObject
    {
        private string PID;
        private int state;
        private int x;
        private int y;
        private int direction;
        private int score;

        public MovableGameObject(string PID, int x, int y, int dir, int state, int score)
        {
            this.PID = PID;
            this.x = x;
            this.y = y;
            this.direction = dir;
            this.state = state;
            this.score = score;
        }
        public MovableGameObject(string PID, int x, int y)
        {
            this.PID = PID;
            this.x = x;
            this.y = y;
        }
        public int getScore()
        {
            return score;
        }
        public string getPID()
        {
            return PID;
        }
        public int getX()
        {
            return x;
        }
        public int getY()
        {
            return y;
        }
        public int getDirection()
        {
            return direction;
        }

        public int getState()
        {
            return state;
        }
    }

    [Serializable]
    class Coin
    {
        int id;
        bool visible;
        int x;
        int y;

        public Coin(int id, bool visible)
        {
            this.id = id;
            this.visible = visible;
        }

        public int getID()
        {
            return id;
        }

        public void setVisible(bool visible)
        {
            this.visible = visible;
        }

        public bool getVisible()
        {
            return visible;
        }

        public int getX()
        {
            return this.x;
        }
        public int getY()
        {
            return this.y;
        }

        public void setX(int x)
        {
            this.x = x;
        }

        public void setY(int y)
        {
            this.y = y;
        }

    }

    [Serializable]
    class Wall
    {
        int x;
        int y;

        public Wall(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int getX()
        {
            return this.x;
        }
        public int getY()
        {
            return this.y;
        }

        public void setX(int x)
        {
            this.x = x;
        }

        public void setY(int y)
        {
            this.y = y;
        }
    }
}
