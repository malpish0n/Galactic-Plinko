using UnityEngine;
using System.Collections.Generic;

public class PlinkoBoardGenerator : MonoBehaviour
{
    [Header("Elements")]
    [Tooltip("Peg/Obstacle prefab that balls bounce off.")]
    [SerializeField] private GameObject pegPrefab;

    [Tooltip("Bucket prefab where balls fall into at the bottom.")]
    [SerializeField] private GameObject bucketPrefab;

    [Tooltip("Wall prefab. Should be a simple Cube scaled appropriately.")]
    [SerializeField] private GameObject wallPrefab;

    [Tooltip("Dropper prefab (ball spawner). Will be moved to the top of the board.")]
    [SerializeField] private GameObject dropperPrefab;

    [Header("Settings")]
    [Tooltip("Number of pyramid rows (this value will increase with upgrades).")]
    [SerializeField] private int pyramidRows = 8;

    [Tooltip("Horizontal spacing between pegs.")]
    [SerializeField] private float spacingX = 0.8f;

    [Tooltip("Vertical spacing between rows.")]
    [SerializeField] private float spacingY = 0.7f;
    
    [Tooltip("Initial number of pegs in the first (top) row.")]
    [SerializeField] private int startPegsCount = 3;

    [Tooltip("Additional wall spacing from pegs (margin). Smaller value means walls are tighter.")]
    [SerializeField] private float wallSpacing = 0.4f;

    // List storing created objects to easily remove them during regeneration
    private List<GameObject> _spawnedPegs = new List<GameObject>();
    private List<GameObject> _spawnedBuckets = new List<GameObject>();
    private List<GameObject> _spawnedWalls = new List<GameObject>();
    private GameObject _spawnedDropper;

    /// <summary>
    /// Generates the board anew. Can be called from another script (e.g., GameManager after purchasing an upgrade).
    /// </summary>
    [ContextMenu("Generate Board")]
    public void GenerateBoard()
    {
        ClearBoard();

        if (pegPrefab == null)
        {
            Debug.LogError("Error: Peg Prefab not assigned in PlinkoBoardGenerator!", this);
            return;
        }

        // --- GENERATING PEGS ---
        int lastRowPegCount = 0;
        float lastRowY = 0f;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float topY = 0f;

        // Loop generating rows (top to bottom)
        for (int row = 0; row < pyramidRows; row++)
        {
            // In typical Plinko, each subsequent row has 1 more peg
            // You can change this logic if you want a different shape
            int pegsInThisRow = startPegsCount + row;
            lastRowPegCount = pegsInThisRow;
            
            // Calculate the width of this specific row to center it perfectly at X=0
            float rowWidth = (pegsInThisRow - 1) * spacingX;
            float startX = -rowWidth / 2f;
            
            // Y position goes down with each row
            float yPos = -row * spacingY; 
            lastRowY = yPos;
            if (row == 0) topY = yPos;

            for (int col = 0; col < pegsInThisRow; col++)
            {
                // Calculate X position for a specific peg
                float xPos = startX + (col * spacingX);
                
                // Track bounds for walls
                if (xPos < minX) minX = xPos;
                if (xPos > maxX) maxX = xPos;

                // Create position relative to the generator object
                Vector3 spawnPos = transform.position + new Vector3(xPos, yPos, 0);
                
                // Instantiation
                GameObject newPeg = Instantiate(pegPrefab, transform);
                newPeg.transform.position = spawnPos;
                newPeg.name = $"Peg_R{row}_C{col}";
                
                _spawnedPegs.Add(newPeg);
            }
        }

        GenerateBuckets(lastRowPegCount, lastRowY);
        GenerateWalls(minX, maxX, topY, lastRowY);
        // Pass topMinX and topMaxX considering margin to UpdateDropperPosition
        // We need to calculate the same thing as in GenerateWalls
        float margin = spacingX * wallSpacing;
        int row0Pegs = startPegsCount;
        float row0Width = (row0Pegs - 1) * spacingX;
        float topMinX = -row0Width / 2f - margin;
        float topMaxX = row0Width / 2f + margin;

        UpdateDropperPosition(topY, topMinX, topMaxX);
    }

    // Create Walls
    private void GenerateWalls(float minX, float maxX, float topY, float bottomY)
    {
        if (wallPrefab == null) return;

        // Smaller margin so walls are closer to the balls
        float margin = spacingX * wallSpacing;
        
        // Calculate extreme points for the first and last row
        // Row 0 (Top)
        int row0Pegs = startPegsCount;
        float row0Width = (row0Pegs - 1) * spacingX;
        float topMinX = -row0Width / 2f;
        float topMaxX = row0Width / 2f;

        // Row Last (Bottom) - use passed minX/maxX from generation loop
        // minX is already the position of the leftmost peg in the last row

        // Wall anchor points
        // Left wall: from (Top-Left) to (Bottom-Left)
        Vector3 topLeft = new Vector3(topMinX - margin, topY, 0); 
        Vector3 bottomLeft = new Vector3(minX - margin, bottomY, 0);

        // Right wall: from (Top-Right) to (Bottom-Right)
        Vector3 topRight = new Vector3(topMaxX + margin, topY, 0);
        Vector3 bottomRight = new Vector3(maxX + margin, bottomY, 0);
        
        // Extend walls a bit up and down so they don't end exactly with pegs
        Vector3 leftDir = (bottomLeft - topLeft).normalized;
        Vector3 rightDir = (bottomRight - topRight).normalized;
        
        float extension = spacingY * 1.5f;

        CreateWallSegment(topLeft - leftDir * extension, bottomLeft + leftDir * extension, "Wall_Left");
        CreateWallSegment(topRight - rightDir * extension, bottomRight + rightDir * extension, "Wall_Right");
    }

    private void CreateWallSegment(Vector3 start, Vector3 end, string name)
    {
        Vector3 centerPos = (start + end) / 2f;
        Vector3 direction = end - start;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject wall = Instantiate(wallPrefab, transform);
        wall.name = name;
        wall.transform.position = transform.position + centerPos;
        wall.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Scale: X is length, Y is thickness (dependent on spacing so it's not too thin/thick)
        wall.transform.localScale = new Vector3(length, spacingX * 0.2f, 1f);
        
        _spawnedWalls.Add(wall);
    }

    private void UpdateDropperPosition(float topY, float leftX, float rightX)
    {
        if (dropperPrefab == null) return;

        // Dropper at the height of the first row, but higher by e.g., 1.5 spacing (instead of 3), to be closer to walls
        float offsetY = spacingY * 1.5f;
        float dropperY = topY + offsetY; 
        Vector3 dropperPos = transform.position + new Vector3(0, dropperY, 0);

        if (_spawnedDropper == null)
        {
            _spawnedDropper = Instantiate(dropperPrefab, dropperPos, Quaternion.identity, transform);
            _spawnedDropper.name = "Dropper";
        }
        else
        {
            _spawnedDropper.transform.position = dropperPos;
        }

        // --- SCALING DROPPER ---
        // Calculate width between walls at topY height (plus slight expansion because walls go outward)
        // For simplicity, we assume width at the top of walls (leftX to rightX is base of walls)
        // Since walls are angled, we must estimate or simply stretch to leftX/rightX
        
        // Reduce dropper width by margin to avoid clipping into walls
        float width = (rightX - leftX) * 0.9f; 
        if (width < 0.1f) width = 0.1f;
        
        // Assume dropper prefab is also a Cube/rectangular cuboid with width 1.
        // We want it to fill space between walls.
        Vector3 currentScale = _spawnedDropper.transform.localScale;
        // Set X scale to width between walls
        _spawnedDropper.transform.localScale = new Vector3(width, currentScale.y, currentScale.z);
    }

    private void GenerateBuckets(int lastRowPegCount, float lastRowY)
    {
        if (bucketPrefab == null) return;

        // Number of buckets should be the same as gaps between pegs of the last row + halves on sides.
        // If we have N pegs in the last row, the bottom of the pyramid has width N-1 gaps.
        // Standardly buckets can be same amount as pegs in last row + 1, but then they stick out.
        // Let's make buckets amount equal to pegs in last row - 1 (so they are only "inside" between pegs) 
        // OR same amount as pegs.
        
        // Let's change logic: Buckets only BETWEEN pegs of the last row.
        int bucketsCount = lastRowPegCount - 1; 

        // If result is 0 (e.g. 1 peg), allow 1 bucket centrally.
        if (bucketsCount < 1) bucketsCount = 1;
        
        // Calculate width for buckets. They are between pegs.
        // Pegs are at positions: X0, X1, X2...
        // Buckets should be at: (X0+X1)/2, (X1+X2)/2...
        
        // Calculate bucket start
        // First peg of last row has (pegsInThisRow - 1) * spacingX / 2 on negative.
        float lastRowWidth = (lastRowPegCount - 1) * spacingX;
        float lastRowStartX = -lastRowWidth / 2f; // position of first peg

        // First bucket should be between 1st and 2nd peg
        // Meaning lastRowStartX + (spacingX / 2)
        
        float bucketStartX = lastRowStartX + (spacingX / 2f);
        
        // Buckets a bit lower than the last row. Increased offset to move them lower.
        float bucketY = lastRowY - (spacingY * 1.5f);

        for (int i = 0; i < bucketsCount; i++)
        {
            float xPos = bucketStartX + (i * spacingX);
             Vector3 spawnPos = transform.position + new Vector3(xPos, bucketY, 0);

             GameObject newBucket = Instantiate(bucketPrefab, transform);
             newBucket.transform.position = spawnPos;
             newBucket.name = $"Bucket_{i}";
             
             _spawnedBuckets.Add(newBucket);
        }
    }

    /// <summary>
    /// Clears the board before generating a new one.
    /// </summary>
    [ContextMenu("Clear Board")]
    public void ClearBoard()
    {
        // 1. Try to remove objects from list (if they exist)
        foreach (var peg in _spawnedPegs)
        {
            if (peg != null)
            {
                // Używamy DestroyImmediate w edytorze, a Destroy w grze, 
                // ale dla bezpieczeństwa edytora tutaj immediate jest ok.
                if (Application.isPlaying) Destroy(peg);
                else DestroyImmediate(peg);
            }
        }
        _spawnedPegs.Clear();

        foreach (var bucket in _spawnedBuckets)
        {
            if (bucket != null)
            {
                 if (Application.isPlaying) Destroy(bucket);
                 else DestroyImmediate(bucket);
            }
        }
        _spawnedBuckets.Clear();

        foreach (var wall in _spawnedWalls)
        {
            if (wall != null)
            {
                 if (Application.isPlaying) Destroy(wall);
                 else DestroyImmediate(wall);
            }
        }
        _spawnedWalls.Clear();

        // Droppera nie usuwamy calkowicie, tylko ewentualnie przesuwamy, ale w ClearBoard mozna go usunac jesli chcemy full reset
        // Decyzja: ClearBoard czysci wszystko co wygenerowane.
        if (_spawnedDropper != null)
        {
             if (Application.isPlaying) Destroy(_spawnedDropper);
             else DestroyImmediate(_spawnedDropper);
             _spawnedDropper = null;
        }

        // 2. Fallback: If list was empty (e.g. after Unity restart), remove all children of the object
        // While loop works best when removing children in editor
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>
    /// Method to call when purchasing an upgrade.
    /// Increases pyramid and regenerates it.
    /// </summary>
    public void UpgradePyramidSize(int splitRowsToAdd)
    {
        pyramidRows += splitRowsToAdd;
        GenerateBoard();
    }
}
