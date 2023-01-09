using UnityEngine;

namespace DoubTech.VisemeAdapter
{
    public class OVRLipsyncAdapter : MonoBehaviour
    {
        [SerializeField] private OVRLipSyncContext lipSyncContext;

        [SerializeField] private string[] visemeMapping = new string[]
        {
            "sil",
            "PP",
            "FF",
            "TH",
            "DD",
            "kk",
            "CH",
            "SS",
            "nn",
            "RR",
            "aa",
            "E",
            "I",
            "O",
            "U"
        };

        private void Start()
        {
            if (null == lipSyncContext)
            {
                lipSyncContext = GetComponent<OVRLipSyncContext>();
            }
        }

        private void Update()
        {
            var frame = lipSyncContext.GetCurrentPhonemeFrame();
            float max = 0;
            int maxIndex = 0;
            for(int i = 0; i < frame.Visemes.Length; i++)
            {
                if (frame.Visemes[i] > max)
                {
                    max = frame.Visemes[i];
                    maxIndex = i;
                }
            }
            
            SendMessage("SetViseme", visemeMapping[maxIndex]);
        }
    }
}