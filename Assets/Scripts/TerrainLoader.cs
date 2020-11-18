using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;

public enum LimitType
{
    Sphere,
    Rect
}

public class TerrainLoader : MonoBehaviour
{
    // 需要带后缀名
    public string HeightDataFile;
    // 显示全部数据时的地形显示尺寸
    public int MapSize;
    // 原始数据的行列数
    public int DataSize;
    //中心点写经纬度，限制范围单位公里
    public Vector2 Center,Limit;
    // 外圈范围粗细和其高度值
    public float OutWidth, OutValue;
    // 查询方式
    public LimitType limit;

    public Terrain terrain;
    public InputField CX, CY, RX, RY;

    private string[] file = null;
    private float detailHeight = 0;
    private float detailWidth = 0;
    private float detailLength = 0;
    private float min = float.PositiveInfinity;
    private float minX = float.PositiveInfinity;
    private float minY = float.PositiveInfinity;
    private float max = 0;
    private float maxX = 0;
    private float maxY = 0;

    private float epsilon = 1e-8f;
    private float EarthRadiusKm = 6378.137f; // WGS-84

    private void Start()
    {
        if(terrain!=null)
        {
            terrain.gameObject.SetActive(true);
            terrain.basemapDistance = 1000000;
        }          

        CX.text = Center.x.ToString();
        CY.text = Center.y.ToString();
        RX.text = Limit.x.ToString();
        RY.text = Limit.y.ToString();

        CX.onValueChanged.AddListener(delegate(string t) {
            var a = float.Parse(CX.text);
            if (a > epsilon)
                Center.x = a;
        });
        CY.onValueChanged.AddListener(delegate (string t) {
            var a = float.Parse(CY.text);
            if (a > epsilon)
                Center.y = a;
        });
        RX.onValueChanged.AddListener(delegate (string t) {
            var a = float.Parse(RX.text);
            if (a > epsilon)
                Limit.x = a;
        });
        RY.onValueChanged.AddListener(delegate (string t) {
            var a = float.Parse(RY.text);
            if (a > epsilon)
                Limit.y = a;
        });
    }

    public void LoadFile(string filename)
    {
        // 读取资源
        string[] fileContents = File.ReadAllLines(Application.dataPath + @"/Data/" + filename);
        file = new string[fileContents.Length];

        for (int i = 0; i < fileContents.Length; i++)
        {
            file[i] = fileContents[i];

            string[] txt = fileContents[i].Split(',');
            var x = float.Parse(txt[2]);
            var y = float.Parse(txt[3]);
            var h = float.Parse(txt[4]);

            if (minX > x)
                minX = x;
            if (maxX < x)
                maxX = x;

            if (minY > y)
                minY = y;
            if (maxY < y)
                maxY = y;

            if (min > h)
                min = h;
            if (max < h)
                max = h;
        }

        // 计算出地形最大值与最小值的高度差
        detailHeight = max - min;
        detailWidth = maxX - minX;
        detailLength = maxY - minY;
    }

    public void Spawn()
    {
        if(file == null)
        {
            LoadFile(HeightDataFile);
        }

        float[,] data = new float[DataSize, DataSize];

        // 填数据
        int startX = int.MaxValue, startY = int.MaxValue, endX = 0, endY = 0;
        int sizeX = 0, sizeY = 0;
        for (int i = 0; i < file.Length; i++)
        {
            string[] txt = file[i].Split(',');
            var x = float.Parse(txt[2]);
            var y = float.Parse(txt[3]);
            var h = float.Parse(txt[4]);

            if (!InLimit(x, y, OutWidth))
                continue;
            else 
            {
                float a1 = (x - minX) / detailWidth * (DataSize - 1);
                float b1 = (y - minY) / detailLength * (DataSize - 1);
                int a = RoundToInt(a1);
                int b = RoundToInt(b1);

                if (!InLimit(x, y))
                {
                    data[a, b] = OutValue;
                }
                else
                {
                    data[a, b] = h;
                }

                if (startX > a)
                    startX = a;
                if (endX < a)
                    endX = a;

                if (startY > b)
                    startY = b;
                if (endY < b)
                    endY = b;
            }                
        }
        sizeX = endX - startX + 1;
        sizeY = endY - startY + 1;
        if (endX==int.MaxValue)
            sizeX = 0;
        if (endY == int.MaxValue)
            sizeY = 0;

        // 0数据的修复，周围8个点取平均
        for (int a = 0; a < sizeX; a++)
        {
            for (int b = 0; b < sizeY; b++)
            {
                if (Mathf.Abs(data[a+startX, b+startY]) > epsilon)
                    continue;

                float t = 0;int num = 0;
                {
                    if (a < sizeX - 1 && Mathf.Abs(data[a + 1 + startX, b + startY]) > epsilon)
                    {
                        t += data[a + 1 + startX, b + startY];
                        num++;
                    }
                    if (a >= 1 && Mathf.Abs(data[a - 1 + startX, b + startY]) > epsilon)
                    {
                        t += data[a - 1 + startX, b + startY];
                        num++;
                    }
                }
                if (b<sizeY-1)
                {
                    if(Mathf.Abs(data[a + startX, b + 1 + startY]) > epsilon)
                    {
                        t += data[a + startX, b + 1 + startY];
                        num++;
                    }
                    if (a < sizeX - 1 && Mathf.Abs(data[a + 1 + startX, b + 1 + startY]) > epsilon)
                    {
                        t += data[a + 1 + startX, b + 1 + startY];
                        num++;
                    }
                    if (a >= 1 && Mathf.Abs(data[a - 1 + startX, b + 1 + startY]) > epsilon)
                    {
                        t += data[a - 1 + startX, b + 1 + startY];
                        num++;
                    }
                }
                if (b >= 1)
                {
                    if(Mathf.Abs(data[a + startX, b - 1 + startY]) > epsilon)
                    {
                        t += data[a + startX, b - 1 + startY];
                        num++;
                    }
                    if (a < sizeX - 1 && Mathf.Abs(data[a + 1 + startX, b - 1 + startY]) > epsilon)
                    {
                        t += data[a + 1 + startX, b - 1 + startY];
                        num++;
                    }
                    if (a >= 1 && Mathf.Abs(data[a - 1 + startX, b - 1 + startY]) > epsilon)
                    {
                        t += data[a - 1 + startX, b - 1 + startY];
                        num++;
                    }
                }
                if(num>4)
                    data[a + startX, b + startY] = t / num;
            }
        }

        // 将地形整体上移，消灭负值
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                if (min < 0)
                {
                    data[i + startX, j + startY] = data[i+startX, j + startY] - min;
                }
                data[i + startX, j + startY] = data[i + startX, j + startY] / detailHeight;
            }
        }

        // 设置地形的最大高度
        terrain.terrainData.size = new Vector3(MapSize, detailHeight, MapSize);
        // 高度图分辨率
        terrain.terrainData.heightmapResolution = DataSize;
        terrain.terrainData.alphamapResolution = DataSize;
        // 导入高度数据
        terrain.terrainData.SetHeights(0, 0, data);
        float[,,] w = new float[terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight, 2];
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                if(Mathf.Abs(data[x + startX, y + startY]) > epsilon)
                {
                    w[x + startX, y + startY, 0] = 1f;
                    w[x + startX, y + startY, 1] = 0f;
                }
                else
                {
                    w[x + startX, y + startY, 0] = 0f;
                    w[x + startX, y + startY, 1] = 1f;
                }               
            }
        }
        terrain.terrainData.SetAlphamaps(0, 0, w);
    }

    // 计算某点是否在范围内
    private bool InLimit(float x, float y, float extra = 0)
    {
        bool a = false;
        if(limit == LimitType.Sphere)
            a= InLimitSphere(x, y, extra);
        else if (limit == LimitType.Rect)
            a = InLimitRect(x, y, extra);
        return a;
    }

    // 计算某点是否在矩形范围内
    private bool InLimitRect(float x,float y,float extra)
    {
        if (GetDistance(x, y, Center.x, y) <= Limit.x+ extra && GetDistance(x, y, x, Center.y) <= Limit.y + extra)
            return true;

        return false;
    }

    // 计算某点是否在圆形范围内
    private bool InLimitSphere(float x, float y, float extra)
    {
        if (GetDistance(x, y, Center.x, Center.y) <= Limit.x+ extra)
            return true;

        return false;
    }

    // 通过经纬度算距离，lat纬度，lng经度
    private float GetDistance(float p1Lat, float p1Lng, float p2Lat, float p2Lng)
    {

        float dLat1InRad = p1Lat * (Mathf.PI / 180);
        float dLong1InRad = p1Lng * (Mathf.PI / 180);
        float dLat2InRad = p2Lat * (Mathf.PI / 180);
        float dLong2InRad = p2Lng * (Mathf.PI / 180);

        float dLongitude = dLong2InRad - dLong1InRad;
        float dLatitude = dLat2InRad - dLat1InRad;

        float a = Mathf.Pow(Mathf.Sin(dLatitude / 2), 2) + Mathf.Cos(dLat1InRad) * Mathf.Cos(dLat2InRad) * Mathf.Pow(Mathf.Sin(dLongitude / 2), 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    // 四舍五入
    private int RoundToInt(float value)
    {
        int a = (int)value;
        if (value - a >= 0.5f)
            return a + 1;
        else
            return a;
    }
}