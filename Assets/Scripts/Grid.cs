using System;
using UnityEngine;

public class Grid : MonoBehaviour {
    
    // Vzájemná vzdálenost od sebe ležících bodů na které se můžeme zarovnat
    private const float length = 1f;
    private const float height = 1.21f;
    private static Vector3 min;
    private static Vector3 max;

    // Vyhradí oblast pro pokládání bloků podle velikosti předané desky.
    public static void SetGridSize(GameObject board) {
        Bounds bounds = board.GetComponent<Collider>().bounds;
        min = bounds.min;
        max = bounds.max;
    }

    // Určí vhodné místo pro vložení bloku. Místo prvně určí kde se bude nacházet střed bloku, následně
    // daný blok posune tak, aby se umisťoval dle jeho krajního bodu, který bude vždy na stejném místě
    // při jakékoliv rotaci.
    public Vector3 SnapToGrid(Vector3 position, Vector3 size, int rotation) {
        Vector3 offset = new Vector3(0.5f,0,0.5f);
        SetParamsForOffset(ref size,ref offset,rotation);
        
        position -= transform.position;
        int x = (int)Math.Round(position.x / length,0,MidpointRounding.AwayFromZero);
        int y = ToleranceFloor(position.y / height);
        int z = (int)Math.Round(position.z / length,0,MidpointRounding.AwayFromZero);
        Vector3 res = new Vector3(x * length - size.x + offset.x, y * height, z * length - size.z + offset.z);
        res += transform.position;
        
        return res;
    }
    
    // Práce se float čísly je někdy nepředvidatelá a nemusí vždy vrátit očekáváné výsledky.
    // Proto je dobré někdy zavést určitou toleranci.
    public static float FloorValue(float value) {
        const float tolerance = 0.001f;
        value = value - Mathf.Round(value) < tolerance ? Mathf.Round(value) : Mathf.Floor(value);
        return value;
    }

    private static int ToleranceFloor(float value) {
        const double tolerance = 0.001f;
        int rounded = Mathf.RoundToInt(value);
        if (Mathf.Abs(value - rounded) >= tolerance) {
            rounded = (int)Mathf.Floor(value);
        }
        return rounded;
    }

    // Připraví hodnoty dle kterých se střed bloku posune tak, aby jeho kraj se zarovnal na místo na desce.
    private static void SetParamsForOffset(ref Vector3 size, ref Vector3 offset, int rotation) {
        switch (rotation) {
            case 90:
                (size.x, size.z) = (size.z, size.x);
                size.z = -size.z;
                offset.x = 0.5f;
                offset.z = -0.5f;
                break;
            case 180:
                size.x = -size.x;
                size.z = -size.z;
                offset.x = -0.5f;
                offset.z = -0.5f;
                break;
            case 270:
                (size.x, size.z) = (size.z, size.x);
                size.x = -size.x;
                offset.x = -0.5f;
                offset.z = 0.5f;
                break;
        }
        size /= 2;
    }

    // Zjistí jestli bude blok mimo desku v x-ové nebo z-kové ose a posune blok tak, aby byl na kraji desky.
    public static bool OutOfBounds(ref Vector3 position, Vector3 size, int rotation) {
        const float halfLength = length / 2;
        if (rotation is 90 or 270) {
            (size.x, size.z) = (size.z, size.x);
        }
        
        Vector3 blockMin = new Vector3(position.x - halfLength * size.x, position.y,position.z - halfLength * size.z);
        Vector3 blockMax = new Vector3(position.x + halfLength * size.x, position.y + height, position.z + halfLength * size.z);
        if (blockMin.y < 0) {
            return true;
        }
        if (blockMin.x < min.x) {
            position.x = position.x + Math.Abs(blockMin.x) - Math.Abs(min.x);
        }
        if (blockMin.z < min.z) {
            position.z = position.z + Math.Abs(blockMin.z) - Math.Abs(min.z);
        }
        if (blockMax.x > max.x) {
            position.x = position.x + max.x - blockMax.x;
        }
        if (blockMax.z > max.z) {
            position.z = position.z + max.z - blockMax.z;
        }
        return false;
    }

    // Pouze pro debugovaní. Vizualizace bodů na které se bloky mohou položit.
    /* private void OnDrawGizmos() {
        if (length > 0) {
            Gizmos.color = Color.yellow;
            for (float x = 0; x >= -16; x -= length) {
                for (float z = 0; z >= -16; z -= length) {
                    var point = SnapToGrid(new Vector3(x, 1f, z),new Vector3(),0);
                    Gizmos.DrawSphere(point,0.1f);
                }
            }
        }
    } */
}

