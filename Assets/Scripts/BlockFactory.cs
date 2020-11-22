using UnityEngine;

public class BlockFactory : MonoBehaviour
{
    public static Block Create(BlockType type, Vector3Int pos)
    {
        switch (type)
        {
            case BlockType.Grass:
                return new Grass(pos);
            case BlockType.Dirt:
                return new Dirt(pos);
            case BlockType.Stone:
                return new Stone(pos);
            case BlockType.BottomStone:
                return new BottomStone(pos);
            case BlockType.Water:
                return new Water(pos);
            case BlockType.Glass:
                return new Glass(pos);
            case BlockType.Tnt:
                return new TNT(pos);
            case BlockType.Lava:
                return new Lava(pos);
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
    Grass = 0,
    Dirt = 1,
    Stone = 2,
    BottomStone = 3,
    Water = 4,
    Glass = 5,
    Tnt = 6,
    Lava = 7,
}
