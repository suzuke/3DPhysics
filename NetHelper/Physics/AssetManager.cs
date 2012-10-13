﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Helper.Physics
{
    public class AssetManager
    {
        public SortedList<int, Asset> Assets = new SortedList<int, Asset>();
        private SortedList<int, Gobject> gameObjects;
        private SortedList<int, Gobject> objectsToAdd;
        private List<int> objectsToDelete;
        public AssetManager(ref SortedList<int, Gobject> gObjects, ref SortedList<int, Gobject> nObjects, ref List<int> dObjects)
        {
            gameObjects = gObjects;
            objectsToAdd = nObjects;
            objectsToDelete = dObjects;
        }

        /// <summary>
        /// Adds an asset
        /// </summary>
        /// <param name="a"></param>
        public void AddAsset(Asset a)
        {
            if (Assets.ContainsKey(a.Name))
                return;
            Assets.Add(a.Name, a);
        }
        /// <summary>
        /// Adds an asset
        /// </summary>
        /// <param name="name"></param>
        /// <param name="CreateCallback"></param>
        /// <param name="scale"></param>
        public void AddAsset(Enum e, GetGobjectDelegate CreateCallback, Vector3 scale)
        {
            int id = (int)Convert.ChangeType(e, e.GetTypeCode());
            AddAsset(new Asset(id, CreateCallback, scale));
        }
        /// <summary>
        /// Adds an asset with a scale of X = Y = Z = scale
        /// </summary>
        /// <param name="name"></param>
        /// <param name="CreateCallback"></param>
        /// <param name="scale"></param>
        public void AddAsset(Enum e, GetGobjectDelegate CreateCallback, float scale)
        {
            AddAsset(e, CreateCallback, new Vector3(scale, scale, scale));
        }
        /// <summary>
        /// Adds an asset with a default scale of 1
        /// </summary>
        /// <param name="name"></param>
        /// <param name="CreateCallback"></param>
        public void AddAsset(Enum e, GetGobjectDelegate CreateCallback)
        {
            AddAsset(e, CreateCallback, 1.0f);
        }

        public Gobject GetNewInstance(Enum e)
        {
            int id = (int)Convert.ChangeType(e, e.GetTypeCode());
            if (!Assets.ContainsKey(id))
                return null;

            Asset a = Assets[id];
            if (a == null)
                return null;

            Gobject go = a.GetNewGobject();
            go.type = id; // THIS IS WRONG BUT SO CHEAP!
            go.ID = GetAvailableObjectId();
            return go;
        }

        /// <summary>
        /// Selects an unused object ID
        /// </summary>
        /// <returns></returns>
        public int GetAvailableObjectId()
        {
            int id = 1;
            bool found = true;
            // locks are expensive, I think.
            // We probably don't want to lock inside a loop.
            lock (gameObjects)
            {
                lock (objectsToAdd)
                {
                    lock (objectsToDelete)
                    {
                        while (found)
                        {
                            if (isObjectIdInUse_Unprotected(id))
                                id++;
                            else
                                found = false;
                        }
                    }
                }
            }
            return id;
        }

        /// <summary>
        /// Does NOT lock on purpose. Must only be called from a method that locks gameObjects, ObjectsToAdd, and then ObjectsToDelete
        /// Allows for good performance when being called iteratively. 
        /// Allows for centralized logic to be called from methods that do the required locking 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool isObjectIdInUse_Unprotected(int id)
        {
            return gameObjects.ContainsKey(id) || objectsToAdd.ContainsKey(id) || objectsToDelete.Contains(id); ;
        }

        /// <summary>
        /// Checks to see if an id is in use already
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool isObjectIdInUse(int id)
        {
            lock (gameObjects)
            {
                lock (objectsToAdd)
                {
                    lock (objectsToDelete)
                    {
                        return isObjectIdInUse_Unprotected(id);
                    }
                }
            }
        }
    }
}
