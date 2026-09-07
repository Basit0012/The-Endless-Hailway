using UnityEngine;

namespace EndlessHallway.Entity
{
    public class ObserverSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string pointId = "FarHallway";
        public string PointId => pointId;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.7f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, new Vector3(0.5f, 2f, 0.5f));
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * 1.2f);
        }
    }
}
