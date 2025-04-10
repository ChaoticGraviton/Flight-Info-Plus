using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Flight.Ui.FlightInfo.Tools
{
    public class FuelRoutines : MonoBehaviour
    {
        public IEnumerator ConfigFuelMonitors()
        {
            yield return new WaitForSeconds(Time.deltaTime * 10);
            Mod.Instance.FlightInfoPlus.FuelInfo.ConfigFuelMonitors();
        }
    }
}