using UnityEngine;

namespace TinyGiantProblems
{
    // Appearance only: the existing tool selection and care rules remain authoritative.
    public sealed class TechnicianToolVisuals : MonoBehaviour
    {
        public TechnicianMotor motor;
        public GameObject vacuum, brush, sprayer, ropeLauncher;
        void LateUpdate()
        {
            if (!motor) return;
            Show(motor.selected);
        }
        public void Show(CareTool tool)
        {
            vacuum.SetActive(tool == CareTool.Vacuum);
            brush.SetActive(tool == CareTool.Groomer);
            sprayer.SetActive(tool == CareTool.Sprayer);
            if(ropeLauncher) ropeLauncher.SetActive(tool == CareTool.Tether);
        }
    }
}
