using System;
using System.Collections.Generic;
using UnityEngine;

public class BlockDatabase : MonoBehaviour { 
    [SerializeField] private List<block> allBlocks;
    private List<BlockData> blocks;

    private void Awake() {
        blocks = new List<BlockData>();
        foreach (var block in allBlocks) {
            blocks.Add(new BlockData(block.obj));
        }
    }

    public GameObject GetBlock(int id) {
        return blocks[id].obj;
    }

    public string GetName(int id) {
        return blocks[id].name;
    }

    public Vector3 GetSize(int id) {
        return blocks[id].size;
    }

    public int GetBlocksCount() {
        return blocks.Count;
    }
}

public class BlockData {
    public GameObject obj;
    public string name;
    public Vector3 size;

    public BlockData(GameObject obj) {
        this.obj = obj;
        name = obj.name;
        size = obj.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh.bounds.size;
        size.y = Mathf.Floor(size.y);
        size.x = (float)Math.Round(size.x,0);
        size.z = (float)Math.Round(size.z,0);
    }
}

[Serializable] public struct block {
    public GameObject obj;
}
