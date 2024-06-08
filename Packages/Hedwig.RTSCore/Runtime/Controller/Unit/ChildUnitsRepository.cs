using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Controller
{
    public class ChildUnitsRepository : ControllerBase, IUnitControllerRepository
    {
        IUnitController[] IUnitControllerRepository.GetUnitControllers()
        {
            var list = new List<IUnitController>();
            foreach(var unitController in transform.GetControllersInChildren<IUnitController>()) {
                list.Add(unitController);
            }
            return list.ToArray();
        }
    }
}
