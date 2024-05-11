using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Controller
{
    public class ChildEnemiesRepository : ControllerBase, IEnemyControllerRepository
    {
        IEnemyController[] IEnemyControllerRepository.GetEnemyController()
        {
            var list = new List<IEnemyController>();
            foreach(var enemyController in transform.GetControllersInChildren<IEnemyController>()) {
                list.Add(enemyController);
            }
            return list.ToArray();
        }
    }
}
