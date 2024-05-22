using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Controller
{
    public class ChildEnemiesRepository : ControllerBase, IEnemyControllerRepository
    {
        IUnitController[] IEnemyControllerRepository.GetEnemyController()
        {
            var list = new List<IUnitController>();
            foreach(var enemyController in transform.GetControllersInChildren<IUnitController>()) {
                list.Add(enemyController);
            }
            return list.ToArray();
        }
    }
}
