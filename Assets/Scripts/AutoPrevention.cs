using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPrevention : MonoBehaviour
{
    #region Singleton
    public static AutoPrevention instance;

    private void Awake() {
        if (instance != null) {
            Debug.Log("AutoPrevention singleton violated!");
            return;
        }

        instance = this;
    }
    #endregion

    private int CPS_MAX = Constants.CPS_MAX;
    private float CLICK_EXPIRY_TIME = Constants.CLICK_EXPIRY_TIME;

    private Dictionary<int, int> RegisteredClicks { get; set; }

    void Start() {
        RegisteredClicks = new Dictionary<int, int>();
    }

    public bool Allowed(Unit _unit) {
        int clicks;
        if (!RegisteredClicks.TryGetValue(_unit.unitId, out clicks)) {
            return true;
        }

        return clicks < CPS_MAX;
    }

    public void RegisterClick(Unit _unit) {
        if (!RegisteredClicks.ContainsKey(_unit.unitId)) {
            RegisteredClicks[_unit.unitId] = 1;
        } else {
            RegisteredClicks[_unit.unitId]++;
        }
        
        StartCoroutine(ExpireClick(_unit.unitId));
    }

    private IEnumerator ExpireClick(int _unitId) {
        yield return new WaitForSeconds(CLICK_EXPIRY_TIME);

        RegisteredClicks[_unitId] = Mathf.Max(RegisteredClicks[_unitId] - 1, 0);
    }
}
