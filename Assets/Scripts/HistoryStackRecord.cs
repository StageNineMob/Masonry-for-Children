using UnityEngine;
using System.Collections;
using System;

namespace StageNine
{
    public interface HistoryKeeper
    {
        bool BatchContinueUndo();
        bool BatchContinueRedo();
        void AddToUndo(HistoryStackRecord record);
        void AddToRedo(HistoryStackRecord record);
        void ClearRedo();
        void Undo();
        void Redo();
    }

    public abstract class HistoryStackRecord
    {
        //enums

        //subclasses

        //consts and static data

        //public data

        //private data
        protected HistoryKeeper historyKeeper;

        //public properties
        public HistoryStackRecord(HistoryKeeper parent)
        {
            historyKeeper = parent;
        }

        //methods
        #region public methods
        public virtual void Do()
        {
            historyKeeper.ClearRedo();
            Execute();
            historyKeeper.AddToUndo(this);
        }

        public virtual void Undo()
        {
            historyKeeper.AddToRedo(this);
            Revert();
        }

        public virtual void Redo()
        {
            historyKeeper.AddToUndo(this);
            Execute();
        }

        public abstract void Execute();

        public abstract void Revert();

        #endregion

        #region private methods

        #endregion

        #region monobehaviors

        #endregion
    }

    public class HSRDrawTile : HistoryStackRecord
    {
        public Color newColor, oldColor;
        public IntVector2 location;
        public GameObject newPrefab, oldPrefab;

        public HSRDrawTile(HistoryKeeper parent, Color color, IntVector2 tileLocation, GameObject prefab)
            :base(parent)
        {
            newColor = color;
            location = tileLocation;
            newPrefab = prefab;

            var tileCheck = MapManager.singleton.GetTileAt(tileLocation);
            if (tileCheck != null)
            {
                oldPrefab = newPrefab; //TODO: make this consider terrain types/ animated types etc.
                oldColor = tileCheck.GetComponent<TileListener>().mainColor;
            }
            else
            {
                oldPrefab = null;
            }
        }


        public override void Execute()
        {
            if(oldPrefab != null)
            {
                MapManager.singleton.OverwriteTile(MapManager.singleton.GetTileAt(location), newPrefab);
                MapManager.singleton.GetTileAt(location).GetComponent<TileListener>().mainColor = newColor;
            }
            else
            {
                MapManager.singleton.InstantiateTile(newPrefab, location).GetComponent<TileListener>().mainColor = newColor;
            }
        }

        public override void Revert()
        {
            if (oldPrefab != null)
            {
                MapManager.singleton.OverwriteTile(MapManager.singleton.GetTileAt(location), oldPrefab);
                MapManager.singleton.GetTileAt(location).GetComponent<TileListener>().mainColor = oldColor;
            }
            else
            {
                MapManager.singleton.DeleteTile(MapManager.singleton.GetTileAt(location));
            }
        }
    }

    public class HSRBatchBegin : HistoryStackRecord
    {
        public HSRBatchBegin(HistoryKeeper parent)
            :base(parent)
        {
        }

        public override void Execute()
        {
            while (historyKeeper.BatchContinueRedo())
            {
                historyKeeper.Redo();
            }
            historyKeeper.Redo();
        }

        public override void Revert()
        {
        }
    }

    public class HSRBatchEnd : HistoryStackRecord
    {
        public HSRBatchEnd(HistoryKeeper parent)
            : base(parent)
        {
        }

        public override void Execute()
        {
        }

        public override void Revert()
        {
            while(historyKeeper.BatchContinueUndo())
            {
                historyKeeper.Undo();
            }
            historyKeeper.Undo();
        }
    }
}
