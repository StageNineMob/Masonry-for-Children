using UnityEngine;
using System.Collections;

namespace StageNine
{
	public class TileModifier
	{
		//Helper tile modifier class, stores special effects on the map, associated tile, effect source

		public interface IModifierSource
		{
			void TileEnter(TileModifier triggeringModifier, GameObject triggeringUnit);
            // void TileLeave(TileModifier triggeringModifier, GameObject triggeringUnit); // Don't know yet if we need this

            bool WillUnitTrigger(GameObject triggeringUnit, bool isAttack);

			void AddModifier(TileModifier newMod); //adds newMod to a list of TileModifiers associated with this source.
			void RemoveModifier(TileModifier modToRemove);
		}

		public enum PathingType
		{
			NONE = 0,
			CLEAR = 1, // tile can be passed freely
			// AVOID, // tile can be passed but doing so has negative effects and requires confirmation
			// "AVOID" creates messy implementation requirements, we may not want to implement it yet or even at all
			STOP = 2, // tile can be entered but triggering the modifier stops movement
			IMPASSABLE = 4//, // tile cannot be entered
            //SLOW = 8 // Tile has increased movement/range cost
		}

		public PathingType pathingType;
		public IModifierSource source;
		public GameObject tile;

        #region constructors

        public TileModifier()
		{
			pathingType = PathingType.NONE;
			source = null;
			tile = null;
		}

		public TileModifier(PathingType pt, IModifierSource ms, GameObject tt)
		{
			pathingType = pt;
			source = ms;
			source.AddModifier(this);
			tile = tt;
			tile.GetComponent<TileListener>().AddModifier(this);
		}

        #endregion

        #region public methods

        public void Cleanup()
		{
			if(source != null)
			{
				source.RemoveModifier(this);
			}
			if(tile != null)
			{
				tile.GetComponent<TileListener>().RemoveModifier(this);
			}
        }

        #endregion
    }
}