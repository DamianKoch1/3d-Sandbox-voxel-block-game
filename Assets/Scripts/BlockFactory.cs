using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockFactory : MonoBehaviour
{
    public static Block Create(BlockType type, Vector3Int pos)
    {
        switch (type)
        {
            case BlockType.grass:
                return new Grass(pos);
            case BlockType.dirt:
                return new Dirt(pos);
            case BlockType.stone:
                return new Stone(pos);
            case BlockType.bottomStone:
                return new BottomStone(pos);
            case BlockType.water:
                return new Water(pos);
            case BlockType.glass:
                return new Glass(pos);
        }
        return null;
    }

    public static Block Create(BlockType type, int x, int y, int z)
    {
        return Create(type, new Vector3Int(x, y, z));
    }
}

public enum BlockType
{
    grass,
    dirt,
    stone,
    bottomStone,
    water,
    glass,
}
