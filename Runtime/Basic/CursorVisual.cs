using UnityEngine;

namespace IDEK.WFC.Basic
{
    [ExecuteAlways]
    public class CursorVisual : MonoBehaviour
    {
        public WFCWorldComponent wfcWorld;

        private void Update()
        {
            if(wfcWorld == null || wfcWorld.cursor == null) return;

            transform.position = wfcWorld.cursor.rawPosition + wfcWorld.transform.position;
            transform.rotation = wfcWorld.transform.rotation;
        }

        private void OnValidate()
        {
            if (wfcWorld == null)
                wfcWorld = FindAnyObjectByType<WFCWorldComponent>();
        }
    }
}
