using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAbility : MonoBehaviour
{

    private AnimancerAbilityLinker linker = null;

    protected AnimancerAbilityLinker Liner {
        get {
            if (linker == null)
                linker = GetComponent<AnimancerAbilityLinker>();
            return linker;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) {
            var player = this.Liner;
            if (player != null) {
                player.TryStartAbility("Test");
            }
        }
    }
}
