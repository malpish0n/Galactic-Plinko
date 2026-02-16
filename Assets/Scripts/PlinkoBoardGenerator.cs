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

    [Tooltip("Padding/gap between MoneyBucket and walls. Increase to make bucket wider (stick to walls), decrease to make it narrower (gap from walls).")]
    [SerializeField] private float bucketToWallPadding = 0.3f;

    // List storing created objects to easily remove them during regeneration
    private List<GameObject> _spawnedPegs = new List<GameObject>();
    private List<GameObject> _spawnedBuckets = new List<GameObject>();
    private List<GameObject> _spawnedWalls = new List<GameObject>();
    private GameObject _spawnedDropper;

    // Cached board extrema used for precise bucket sizing/positioning
    private float _wallTopLeftX = 0f;
    private float _wallTopRightX = 0f;
    private float _wallBottomLeftX = 0f;
    private float _wallBottomRightX = 0f;
    private float _wallTopY = 0f;
    private float _wallBottomY = 0f;

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

        // Cache wall extrema so buckets can be sized exactly to walls
        float margin = spacingX * wallSpacing;
        int row0Pegs = startPegsCount;
        float row0Width = (row0Pegs - 1) * spacingX;
        // top positions (including margin)
        _wallTopLeftX = -row0Width / 2f - margin;
        _wallTopRightX = row0Width / 2f + margin;
        _wallTopY = topY;
        // bottom positions (including margin)
        _wallBottomLeftX = minX - margin;
        _wallBottomRightX = maxX + margin;
        _wallBottomY = lastRowY;

        GenerateBuckets(lastRowPegCount, lastRowY);
        GenerateWalls(minX, maxX, topY, lastRowY);
        // Use cached wall extrema for dropper sizing/position
        UpdateDropperPosition(topY, _wallTopLeftX, _wallTopRightX);
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
        // Check if bucket already exists in scene
        GameObject existingBucket = null;
        
        // First check in list
        if (_spawnedBuckets.Count > 0 && _spawnedBuckets[0] != null)
        {
            existingBucket = _spawnedBuckets[0];
        }
        else
        {
            // Search in hierarchy
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Bucket") || child.name.Contains("bucket"))
                {
                    existingBucket = child.gameObject;
                    _spawnedBuckets.Clear();
                    _spawnedBuckets.Add(existingBucket);
                    break;
                }
            }
            
            // Search in entire scene as fallback
            if (existingBucket == null)
            {
                MoneyBucket[] bucketsInScene = Object.FindObjectsByType<MoneyBucket>(FindObjectsSortMode.None);
                if (bucketsInScene.Length > 0)
                {
                    existingBucket = bucketsInScene[0].gameObject;
                    _spawnedBuckets.Clear();
                    _spawnedBuckets.Add(existingBucket);
                }
            }
        }

        // If bucket exists, just update it
        if (existingBucket != null)
        {
            Debug.Log($"[PlinkoBoardGenerator] Found existing MoneyBucket, updating position and scale...");
            UpdateMoneyBucketsPosition(lastRowPegCount, lastRowY);
            return;
        }

        // If bucket doesn't exist, create new one (only on first Generate Board)
        if (bucketPrefab == null)
        {
            Debug.LogWarning("[PlinkoBoardGenerator] No existing MoneyBucket found and bucketPrefab is null!");
            return;
        }

        // One bucket in the center, which stretches to full width
        
        // Buckets a bit lower than the last row. Increased offset to move them lower.
        float bucketY = lastRowY - (spacingY * 1.5f);
        
        // Calculate wall positions at bucket height (in local generator coordinates)
        GetWallXsAtY(bucketY, out float leftLocalX, out float rightLocalX);
        
        // Calculate bucket width - from wall to wall
        // Include wall thickness and padding to adjust gap
        float wallThickness = spacingX * 0.2f;
        float bucketWidth = Mathf.Abs(rightLocalX - leftLocalX) + (wallThickness + bucketToWallPadding) * 2f;
        
        // Position in center (X=0)
        Vector3 spawnPos = transform.position + new Vector3(0, bucketY, 0);

        GameObject newBucket = Instantiate(bucketPrefab, transform);
        newBucket.transform.position = spawnPos;
        newBucket.name = "MoneyBucket";
        
        // Scale bucket to board width (from wall to wall)
        Vector3 currentScale = newBucket.transform.localScale;
        newBucket.transform.localScale = new Vector3(bucketWidth, currentScale.y, currentScale.z);
        
        _spawnedBuckets.Add(newBucket);
        
        Debug.Log($"[PlinkoBoardGenerator] Created NEW MoneyBucket with width: {bucketWidth} (leftX={leftLocalX}, rightX={rightLocalX}, padding={bucketToWallPadding})");
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
                // Use DestroyImmediate in editor, Destroy in game
                // but for editor safety immediate is ok here.
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

        // We don't remove dropper completely, we could just move it, but ClearBoard can remove it if we want full reset
        // Decision: ClearBoard clears everything that was generated.
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

    /// <summary>
    /// Expands the board while preserving existing Dropper and MoneyBucket.
    /// Removes only pegs and walls, then regenerates them for the larger board.
    /// </summary>
    [ContextMenu("Expand Board")]
    public void ExpandBoard(int rowsToAdd = 1)
    {
        pyramidRows += rowsToAdd;
        
        // Remove only pegs and walls
        ClearPegsAndWalls();
        
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

        for (int row = 0; row < pyramidRows; row++)
        {
            int pegsInThisRow = startPegsCount + row;
            lastRowPegCount = pegsInThisRow;
            
            float rowWidth = (pegsInThisRow - 1) * spacingX;
            float startX = -rowWidth / 2f;
            
            float yPos = -row * spacingY; 
            lastRowY = yPos;
            if (row == 0) topY = yPos;

            for (int col = 0; col < pegsInThisRow; col++)
            {
                float xPos = startX + (col * spacingX);
                
                if (xPos < minX) minX = xPos;
                if (xPos > maxX) maxX = xPos;

                Vector3 spawnPos = transform.position + new Vector3(xPos, yPos, 0);
                
                GameObject newPeg = Instantiate(pegPrefab, transform);
                newPeg.transform.position = spawnPos;
                newPeg.name = $"Peg_R{row}_C{col}";
                
                _spawnedPegs.Add(newPeg);
            }
        }

        // Cache wall extrema for updated board
        float margin = spacingX * wallSpacing;
        int row0Pegs = startPegsCount;
        float row0Width = (row0Pegs - 1) * spacingX;
        _wallTopLeftX = -row0Width / 2f - margin;
        _wallTopRightX = row0Width / 2f + margin;
        _wallTopY = topY;
        _wallBottomLeftX = minX - margin;
        _wallBottomRightX = maxX + margin;
        _wallBottomY = lastRowY;

        // Regenerate only walls
        GenerateWalls(minX, maxX, topY, lastRowY);
        
        // Update MoneyBuckets position to new position
        UpdateMoneyBucketsPosition(lastRowPegCount, lastRowY);
        
        // Update dropper position for new board (use cached wall values)
        UpdateDropperPosition(topY, _wallTopLeftX, _wallTopRightX);
        
        Debug.Log($"[PlinkoBoardGenerator] Board expanded to {pyramidRows} rows. Dropper and MoneyBuckets repositioned.");
    }

    /// <summary>
    /// Removes only pegs and walls, preserving Dropper and MoneyBuckets.
    /// </summary>
    private void ClearPegsAndWalls()
    {
        // Remove pegs
        foreach (var peg in _spawnedPegs)
        {
            if (peg != null)
            {
                if (Application.isPlaying) Destroy(peg);
                else DestroyImmediate(peg);
            }
        }
        _spawnedPegs.Clear();

        // Remove walls
        foreach (var wall in _spawnedWalls)
        {
            if (wall != null)
            {
                if (Application.isPlaying) Destroy(wall);
                else DestroyImmediate(wall);
            }
        }
        _spawnedWalls.Clear();
    }

    /// <summary>
    /// Given a world-space Y position, returns the left and right X coordinates of the walls at that Y
    /// by linearly interpolating between the top and bottom wall anchor points.
    /// </summary>
    private void GetWallXsAtY(float y, out float leftX, out float rightX)
    {
        // Protect against degenerate case where top and bottom Y are equal
        if (Mathf.Approximately(_wallTopY, _wallBottomY))
        {
            leftX = _wallTopLeftX;
            rightX = _wallTopRightX;
            return;
        }

        float t = (y - _wallTopY) / (_wallBottomY - _wallTopY);
        t = Mathf.Clamp01(t);

        leftX = Mathf.Lerp(_wallTopLeftX, _wallBottomLeftX, t);
        rightX = Mathf.Lerp(_wallTopRightX, _wallBottomRightX, t);
    }

    /// <summary>
    /// Updates the position and scale of a single MoneyBucket after expansion.
    /// Expands the bucket to the new board width.
    /// </summary>
    private void UpdateMoneyBucketsPosition(int lastRowPegCount, float lastRowY)
    {
        // If list is empty, find bucket
        if (_spawnedBuckets.Count == 0 || _spawnedBuckets[0] == null)
        {
            Debug.LogWarning("[PlinkoBoardGenerator] _spawnedBuckets list is empty! Searching for bucket...");
            
            _spawnedBuckets.Clear();
            
            // First search in generator hierarchy
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Bucket") || child.name.Contains("bucket"))
                {
                    _spawnedBuckets.Add(child.gameObject);
                    break; // Only one bucket
                }
            }
            
            // If not found, search in entire scene
            if (_spawnedBuckets.Count == 0)
            {
                MoneyBucket[] bucketsInScene = Object.FindObjectsByType<MoneyBucket>(FindObjectsSortMode.None);
                if (bucketsInScene.Length > 0)
                {
                    _spawnedBuckets.Add(bucketsInScene[0].gameObject);
                    Debug.Log($"[PlinkoBoardGenerator] Found MoneyBucket in scene: {bucketsInScene[0].name}");
                }
            }
            
            if (_spawnedBuckets.Count == 0)
            {
                Debug.LogError("[PlinkoBoardGenerator] No MoneyBucket found in hierarchy or scene!");
                return;
            }
            
            Debug.Log($"[PlinkoBoardGenerator] Found MoneyBucket.");
        }

        // Should be only one bucket
        if (_spawnedBuckets.Count > 1)
        {
            Debug.LogWarning($"[PlinkoBoardGenerator] Found {_spawnedBuckets.Count} buckets, but should be only 1. Using first one.");
        }

        GameObject bucket = _spawnedBuckets[0];
        if (bucket == null)
        {
            Debug.LogError("[PlinkoBoardGenerator] MoneyBucket reference is null!");
            return;
        }

        // Calculate new Y position for bucket
        float bucketY = lastRowY - (spacingY * 1.5f);

        // Calculate wall positions at bucket height (in local generator coordinates)
        GetWallXsAtY(bucketY, out float leftLocalX, out float rightLocalX);

        // Calculate bucket width - from wall to wall
        // Include wall thickness and padding to adjust gap
        float wallThickness = spacingX * 0.2f;
        float bucketWidth = Mathf.Abs(rightLocalX - leftLocalX) + (wallThickness + bucketToWallPadding) * 2f;
        
        // Position in center (X=0, because walls are symmetric around center)
        Vector3 newPos = transform.position + new Vector3(0, bucketY, 0);
        bucket.transform.position = newPos;

        // Set bucket width
        Vector3 currentScale = bucket.transform.localScale;
        bucket.transform.localScale = new Vector3(bucketWidth, currentScale.y, currentScale.z);

        Debug.Log($"[PlinkoBoardGenerator] Updated MoneyBucket position to {newPos} and width to {bucketWidth} (leftX={leftLocalX}, rightX={rightLocalX}, padding={bucketToWallPadding})");
    }
}
