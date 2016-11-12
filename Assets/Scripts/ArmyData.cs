using UnityEngine;
using System.Collections.Generic;
using System;

namespace StageNine
{

    public class ArmyData
    {
        //enums
        public enum UnitType
        {
            NONE = 0,
            PAWN = 1,
            KNIGHT = 2,
            ARCHER = 3,
            BIKER = 4,
            ARTILLERY = 5,
            BATTLEMAGE_LUMP = 100
        }

        public const UnitType FIRST_BATTLEMAGE = UnitType.BATTLEMAGE_LUMP;

        //subclasses

        //consts and static data
        public static readonly UnitType[] febaPriority = { UnitType.KNIGHT, UnitType.PAWN, UnitType.BIKER, UnitType.ARCHER, UnitType.ARTILLERY, UnitType.BATTLEMAGE_LUMP }; //FEBA (forward edge of battle area) strategic priority of unit placement list.
        //public static Dictionary<UnitType, int> pointCost;

        //public data
        public string armyName;

        //private data
        private Dictionary<UnitType, int> _units;
        private int _pointTotal;

        //properties
        public Dictionary<UnitType, int> units 
        {
            get 
            {
                return new Dictionary<UnitType, int>(_units);
            }
        }

        public int armySize
        {
            get
            {
                var sum = 0;
                foreach(var pair in _units)
                {
                    sum += pair.Value;
                }
                return sum;
            }
        }

        public int pointTotal 
        {
            get 
            {
                return _pointTotal;
            } 
        }
        //methods

        public ArmyData()
        {
            _units = new Dictionary<UnitType,int>();
            armyName = "";
        }

        public ArmyData(ArmyData rSrc)
        {
            _units = new Dictionary<UnitType, int>(rSrc._units);
            armyName = rSrc.armyName;
        }

        public ArmyData(List<GameObject> unitList)
        {
            _units = new Dictionary<UnitType, int>();
            armyName = "";

            foreach(var unit in unitList)
            {
                AddUnit(unit.GetComponent<UnitHandler>().unitType);
            }
        }

        #region public methods

        // MAYBEDO: Make an editable file format where we can import data here rather than hardcoding
        // like a spreadsheet scraper or something
        public static int PointCost(UnitType type)
        {
            switch (type)
            {
                case UnitType.PAWN:
                    return 10;
                case UnitType.KNIGHT:
                    return 15;
                case UnitType.BIKER:
                    return 20;
                case UnitType.ARCHER:
                    return 25;
                case UnitType.ARTILLERY:
                    return 30;
                default:
                    return 0;
            }
        }

        public static string GetNameOfType(UnitType type)
        {
            switch(type)
            {
                case UnitType.PAWN:
                    return "Pawn";
                case UnitType.KNIGHT:
                    return "Knight";
                case UnitType.BIKER:
                    return "Biker";
                case UnitType.ARCHER:
                    return "Archer";
                case UnitType.ARTILLERY:
                    return "Artillery";
                default:
                    return GetNameOfBattleMage(type);
            }
        }

        public static string GetNameOfBattleMage(UnitType type)
        {
            switch (type)
            {
                case UnitType.BATTLEMAGE_LUMP:
                    return "Lump";
                default:
                    Debug.LogError("[ArmyData:GetNameOfBattleMage] Not a Battlemage!");
                    return "ERROR";
            }

        }


        /// <summary>
        /// Returns this army's Battlemage
        /// </summary>
        /// <returns></returns>
        public UnitType GetBattleMage()
        {
            foreach(var pair in _units)
            {
                if(pair.Key >= FIRST_BATTLEMAGE)
                {
                    return pair.Key;
                }
            }
            return UnitType.NONE;
        }

        public void SetBattlemage(UnitType unit)
        {
            foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
            {
                if (type >= FIRST_BATTLEMAGE)
                {
                    if(type == unit)
                    {
                        if (_units.ContainsKey(unit))
                        {
                            _units[unit] = 1;
                        }
                        else
                        {
                            _units.Add(unit, 1);
                        }
                    }
                    else
                    {
                        if (_units.ContainsKey(unit))
                        {
                            _units.Remove(unit);
                        }
                    }
                }
            }
        }

        public void AddUnit(UnitType unit)
        {
            if (_units.ContainsKey(unit))
            {
                _units[unit]++;
            }
            else
            {
                _units.Add(unit, 1);
            }
            _pointTotal += PointCost(unit);
        }

        public void AddUnit(UnitType unit, int count)
        {
            if (_units.ContainsKey(unit))
            {
                _units[unit] += count;
            }
            else
            {
                _units.Add(unit, count);
            }
            _pointTotal += PointCost(unit) * count;
        }

        public void RemoveUnit(UnitType unit)
        {
            if (_units.ContainsKey(unit))
            {
                if (_units[unit] == 1)
                {
                    _units.Remove(unit);
                }
                else
                {
                    _units[unit]--;
                }
                _pointTotal -= PointCost(unit);
            }
            else
            {
                Debug.LogError("[ArmyData:RemoveUnit] Can't remove a unit you don't have!");
            }
        }

        public void RemoveAllOfType(UnitType type)
        {
            if (_units.ContainsKey(type))
            {
                _pointTotal -= PointCost(type) * _units[type];
                _units.Remove(type);
            }
            else
            {
                Debug.LogError("[ArmyData:RemoveAllOfType] Can't remove a unit type you don't have!");
            }
        }

        public int GetCountOfType(UnitType type)
        {
            if (_units.ContainsKey(type))
            {
                return _units[type];
            }
            else
                return 0;
        }

        public void Clear()
        {
            _units.Clear();
            _pointTotal = 0;
        }

        
        #endregion

        #region private methods

        #endregion

    }

    [Serializable]
    public class SerializableArmy
    {
        public string armyName;
        public ArmyData.UnitType[] types;
        public int[] counts;
        #region public methods
        public override string ToString()
        {
            string output = armyName;
            for (int ii = 0; ii < counts.Length; ++ii)
            {
                output += "\n   " + types[ii].ToString() + ": " + counts[ii];
            }
            return output;
        }
        #endregion

    }
}
