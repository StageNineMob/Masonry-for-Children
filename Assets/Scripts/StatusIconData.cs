using UnityEngine;
using System.Collections;

namespace StageNine
{
    public class StatusIconData
    {
        //public data
        public Sprite sprite;
        public Color color;
        //private data

        //properties





        //methods


        public StatusIconData()
        {

        }

        public StatusIconData(StatusIconData data)
        {
            sprite = data.sprite;
            color = data.color;
        }

        public StatusIconData(Sprite inSprite, Color inColor)
        {
            sprite = inSprite;
            color = inColor;
        }

        public static bool operator == (StatusIconData first, StatusIconData second)
        {
            if (first.color == second.color && first.sprite == second.sprite)
                return true;
            return false;
        }

        public static bool operator != (StatusIconData first, StatusIconData second)
        {
            return !(first == second);
        }

        public override bool Equals(object obj)
        {
            if (obj is StatusIconData)
            {
                return this == (StatusIconData)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (color.GetHashCode() ^ sprite.GetHashCode());
        }

    }
}
