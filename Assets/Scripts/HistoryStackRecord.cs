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
        public ColorPickerPopup.SwatchData newSwatch, oldSwatch;
        public IntVector2 location;
        public GameObject newPrefab, oldPrefab;

        public HSRDrawTile(HistoryKeeper parent, ColorPickerPopup.SwatchData swatch, IntVector2 tileLocation, GameObject prefab)
            :base(parent)
        {
            newSwatch = swatch;
            location = tileLocation;
            newPrefab = prefab;

            var tileCheck = MapManager.singleton.GetTileAt(tileLocation);
            if (tileCheck != null)
            {
                oldPrefab = newPrefab; //TODO: make this consider terrain types/ animated types etc.
                oldSwatch = tileCheck.GetComponent<TileListener>().swatchData;
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
                MapManager.singleton.GetTileAt(location).GetComponent<TileListener>().swatchData = newSwatch;
            }
            else
            {
                MapManager.singleton.InstantiateTile(newPrefab, location).GetComponent<TileListener>().swatchData = newSwatch;
            }
        }

        public override void Revert()
        {
            if (oldPrefab != null)
            {
                MapManager.singleton.OverwriteTile(MapManager.singleton.GetTileAt(location), oldPrefab);
                MapManager.singleton.GetTileAt(location).GetComponent<TileListener>().swatchData = oldSwatch;
            }
            else
            {
                MapManager.singleton.DeleteTile(MapManager.singleton.GetTileAt(location));
            }
        }
    }


    public class HSREraseTile : HistoryStackRecord
    {
        public ColorPickerPopup.SwatchData oldSwatch;
        public IntVector2 location;
        public GameObject oldPrefab;

        public HSREraseTile(HistoryKeeper parent, IntVector2 tileLocation)
            : base(parent)
        {
            location = tileLocation;

            var tileCheck = MapManager.singleton.GetTileAt(tileLocation);
            if (tileCheck != null)
            {
                //MapManager.BrushType oldType = tileCheck.GetComponent<TileListener>().
                oldPrefab = MapManager.singleton.GetTilePrefabForType(MapManager.BrushType.TILE); //TODO: make this consider terrain types/ animated types etc.
                oldSwatch = tileCheck.GetComponent<TileListener>().swatchData;
            }
            else
            {
                oldPrefab = null;
            }
        }

        public override void Execute()
        {
            if (oldPrefab != null)
            {
                MapManager.singleton.DeleteTile(MapManager.singleton.GetTileAt(location));
            }
        }

        public override void Revert()
        {
            if (oldPrefab != null)
            {
                MapManager.singleton.InstantiateTile(oldPrefab, location).GetComponent<TileListener>().swatchData = oldSwatch;
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
