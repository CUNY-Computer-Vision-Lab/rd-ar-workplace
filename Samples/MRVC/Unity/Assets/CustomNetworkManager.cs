using UnityEngine;
using UnityEngine.Networking;

public class CustomNetworkManager : MonoBehaviour
{
    public int port;
    public NetworkIdentity prefab;
    public GameObject clientPrefab;
    public GameObject SceneRoot;
    public DrawLine lineHandler;
    public GameObject SpatialMeshVR;
    public Material lineMaterial;
    public Material surfaceMaterial;

    public bool isAtStartup = true;
    public bool isTheClient = true;
    public string serverAddress;
    public static PhotonView photonView;

    public class CustomMessage
    {
        public static short lineMessage = MsgType.Highest + 1;
    }
    public class AssetMessage
    {
        public static short assetMessage = MsgType.Highest + 2;
    }
    public class SpatialMeshMsg
    {
        public static short meshMsg = MsgType.Highest + 3;
    }

    public class PointsMessage : MessageBase
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] triangles;
    }
    public class ARLineMessage : MessageBase
    {
        public Vector3[] pointPositions;
    }

    public class AssetsMessage : MessageBase
    {
        public NetworkHash128 assetId;
    }

    public void sendSpatialMesh(Mesh mesh)
    {
        var data = MeshSerializer.WriteMesh(mesh, true);

        photonView.RPC("TransferMesh", PhotonTargets.All, SpatialMeshMsg.meshMsg, data);
    }

    public void sendARLine(Vector3[] pointPositions)
    {
        ARLineMessage message = new ARLineMessage();
        message.pointPositions = pointPositions;

        photonView.RPC("TransferARLine", PhotonTargets.All, CustomMessage.lineMessage, pointPositions);
    }

    void Start()
    {
        photonView = PhotonView.Get(this);

        ConnectionConfig myConfig = new ConnectionConfig();
        Debug.Log("Channels: " + myConfig.ChannelCount);
        foreach (ChannelQOS channel in myConfig.Channels)
        {
            Debug.Log(channel);
        }
        
        if (isTheClient)
        {
            Debug.Log("STARTING CLIENT CustomNetworkManager");
        }
        else
        {
            Debug.Log("STARTING SERVER");
        }
    }

    [PunRPC]
    public void TransferMesh(NetworkMessage netMsg)
    {
        Debug.Log("Received spatial mesh message");
        PointsMessage message = netMsg.ReadMessage<PointsMessage>();

        // line object
        GameObject surfaceObject = new GameObject("Surface Object");
        surfaceObject.transform.SetParent(SpatialMeshVR.transform);

        Mesh mesh = new Mesh();
        surfaceObject.AddComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = message.vertices;
        mesh.uv = message.uvs;
        mesh.triangles = message.triangles;
        surfaceObject.AddComponent<MeshRenderer>().material = surfaceMaterial;
    }

    [PunRPC]
    public void ClientConnected()
    {
        Debug.Log("Client connected: " + MsgType.Connect);
    }

    [PunRPC]
    public void TransferAssetMessage(NetworkMessage netMsg)
    {
        Debug.Log("Asset Message: " + AssetMessage.assetMessage);
        
        AssetsMessage msg = netMsg.ReadMessage<AssetsMessage>();
        foreach (NetworkHash128 hash in ClientScene.prefabs.Keys)
        {
            if (hash.Equals(prefab.assetId))
            {
                Debug.Log("Found match");
                return;
            }
        }

        Debug.Log("Registering spawn handler for prefab");
        Debug.Log("Readying client");
        Debug.Log(msg);

        ClientScene.Ready(netMsg.conn);
    }

    [PunRPC]
    public void TransferLineMessage(Mesh mesh)
    {
        Debug.Log("Line Message: " + CustomMessage.lineMessage);

        GameObject line = new GameObject();
        line.AddComponent<MeshFilter>().mesh = mesh;
        line.AddComponent<MeshRenderer>().material = lineMaterial;
    }
}
