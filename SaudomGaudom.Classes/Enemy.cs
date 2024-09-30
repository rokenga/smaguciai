namespace SaudomGaudom.Classes
{
    public class Enemy
    {
        private int _id;
        private double _speed;
        private string _color;
        private double _currentX;
        private double _currentY;

        public Enemy(int id, string color, double speed = 2)
        {
            _id = id;
            _color = color;
            _speed = speed;
            SetCurrentPosition(0, 0);

        }

        public int GetId()
        {
            return _id;
        }

        public string GetColor()
        {
            return _color;
        }

        public double GetCurrentX()
        {
            return _currentX;
        }

        public double GetCurrentY()
        {
            return _currentY;
        }

        public double GetSpeed()
        {
            return _speed;
        }

        public void SetCurrentPosition(double x, double y)
        {
            _currentX = x;
            _currentY = y;
        }
        public override bool Equals(object obj)
        {
            if (obj is Enemy otherPlayer)
            {
                return this.GetId() == otherPlayer.GetId();
            }
            return false;
        }
        public override int GetHashCode()
        {
            return this.GetId().GetHashCode();
        }
        public override string ToString()
        {
            return $"{GetId()}:{GetColor()}:{GetCurrentX()}:{GetCurrentY()}";
        }
    }
}
