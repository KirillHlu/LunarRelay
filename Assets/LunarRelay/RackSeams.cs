using UnityEngine;
namespace LunarRelay {
public sealed partial class GameSession {
 Transform rackCap,rackGasket;int removedUpperRackBars;
 void RebuildRackCap(Transform rack){
  rackCap=Find(rack,"Top_InsulatedCap");rackGasket=Find(rack,"Top_NavySeam");
  foreach(var part in rack.GetComponentsInChildren<Transform>())if(part.name=="Frame_SideTie_Port_1.83"||part.name=="Frame_SideTie_Starboard_1.83"||part.name=="Frame_FrontCrossbar_1.83"||part.name=="Frame_RearCrossbar_1.83"){part.gameObject.SetActive(false);removedUpperRackBars++;}
  foreach(var part in new[]{rackCap,rackGasket}){
   var filter=part.GetComponent<MeshFilter>();var bounds=filter.sharedMesh.bounds;var size=bounds.size+new Vector3(part==rackCap?.020f:.015f,0,part==rackCap?.020f:.010f);
   var mesh=RoundedForms.Box(size,part==rackCap?.008f:.002f);mesh.name=part.name+" continuous overhanging shell";var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]+=bounds.center;mesh.vertices=vertices;mesh.RecalculateBounds();filter.sharedMesh=mesh;
  }
 }
}
}
