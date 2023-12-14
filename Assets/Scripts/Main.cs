using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Main : MonoBehaviour
{ 
    // Only used to call MonoBehaviour.StartCoroutine, which isn't static for some reason
    static Main refer;
    void Awake() => refer = this;
    

    public static Node[,] LastCreatedMap;
    public static Node[,] CreateMap(Vector2Int size, bool hex)
    {
        Node[,] map = new Node[size.x, size.y];

        for (int x = 0; x < map.GetLength(0); x++)
            for (int y = 0; y < map.GetLength(1); y++)
                map[x, y] = hex ? new Node.Hex { Position = new Vector2Int(x,y)} : new Node.Rect { Position = new Vector2Int(x,y)};

        LastCreatedMap = map;
        return map;
    }
    public abstract class Node
    {
        /// <summary> Indicates in which directions we're open/closed. Used to determine which sprite to use </summary>
        public abstract bool[] Openings { get; }
        
        /// <summary> Indicates in which directions we've went from here, during generation. So as to not go the same way twice </summary>
        public abstract bool[] Went { get; }
        public abstract Vector2Int Position { get; set; }
        
        
        public class Rect : Node
        {          
            // Direction names: 
            // N, E, S, W
            
            public override bool[] Openings { get; } = { false, false, false, false };
            public override bool[] Went { get; } = { false, false, false, false };
            public override Vector2Int Position { get; set; }
        }
        public class Hex : Node
        {
            // Direction names: 
            // NE, SE, S, SW, NW, N
            
            public override bool[] Openings { get; } = { false, false, false, false, false, false };
            public override bool[] Went { get; } = { false, false, false, false, false, false };
            public override Vector2Int Position { get; set; }
        }
    }
    
    


    static Coroutine startedRunCoroutine;
    static readonly List<GameObject> SpawnedCompletionTrails = new();
    
    public static void ClearSpawnedCompletionTrials()
    {
        foreach(GameObject go in SpawnedCompletionTrails)
            Destroy(go);
        SpawnedCompletionTrails.Clear();
    }



    /// <summary> This class contains delegates to manipulate the Run after starting it </summary>
    public class RunInterface
    {
        public Action Cancel;
        public Action<float> AdjustWaitTime;
    } 
    public struct RunParams { public Vector2Int Size, Start, End; }


    /// <summary> Gives new position based on the direction you wanna go </summary>
    /// <param name="direction"> The direction name </param>
    /// <param name="position"> Current position </param>
    /// <param name="hex"> Set to true for use with hex nodes </param>
    /// <returns> New position </returns>
    static Vector2Int Project(string direction, Vector2Int position, bool hex)
    {
        if (hex)
        {
            bool isYEven = position.y % 2 == 0;
            return direction switch
            {
                "NE" when isYEven => position + new Vector2Int(0, 1),
                "NE" => position + new Vector2Int(1, 1),

                "SE" when isYEven => position + new Vector2Int(-1, 1),
                "SE" => position + new Vector2Int(0, 1),

                "S" => position + new Vector2Int(-1, 0),

                "SW" when isYEven => position + new Vector2Int(-1, -1),
                "SW" => position + new Vector2Int(0, -1),

                "NW" when isYEven => position + new Vector2Int(0, -1),
                "NW" => position + new Vector2Int(1, -1),

                "N" => position + new Vector2Int(1, 0)
            };
        }
        else
        {
            return direction switch
            {
                "N" => position + new Vector2Int(0, 1),
                "E" => position + new Vector2Int(1, 0),
                "S" => position + new Vector2Int(0, -1),
                "W" => position + new Vector2Int(-1, 0)
            };
        }
    }
    
    /// <summary> Runs the maze generator </summary>
    /// <param name="runParams"></param>
    /// <param name="hex">Whether you're using hex tiles</param>
    /// <param name="completionCallback">Will get called upon completion</param>
    /// <returns> Contains delegates to manipulate this Run after starting it </returns>
    public static RunInterface Run(RunParams runParams, bool hex, Action completionCallback = null)
    {
        if (startedRunCoroutine != null)
            refer.StopCoroutine(startedRunCoroutine);

        // Amount of seconds to wait after every step
        float waitTime = 0.02f;
        
        startedRunCoroutine = refer.StartCoroutine(RunCoroutine());
        
        // Configure what happens when this Run is cancelled or has it's wait time adjusted, and return that
        return new RunInterface
        {
            Cancel = () =>
            {
                refer.StopCoroutine(startedRunCoroutine);
                completionCallback?.Invoke();
            },
            AdjustWaitTime = value =>
            {
                // Clamp
                value = value switch
                {
                    < 0.01f => 0.01f,
                    > 1f => 1f,
                    _ => value
                };
                waitTime = value;
            }
        };
        
        IEnumerator RunCoroutine()
        {
            Node[,] map = CreateMap(runParams.Size, hex);
            map.FocusCamera();
            List<Node> allVisited = new() { map[runParams.Start.x, runParams.Start.y] };
            List<Node> path       = new() { map[runParams.Start.x, runParams.Start.y] };
            
            while (true)
            {
                Node current = path.Last();

                string[] moveOptions = !hex ? new []{ "N", "E", "S", "W" } : new[]{ "NE", "SE", "S", "SW", "NW", "N" };
                
                // Remove movement options that are in a direction this node has already went to
                for (int i = 0; i < moveOptions.Length; i++)
                    if (current.Went[i])
                        moveOptions[i] = "-";
                
                // Remove movement options that lead out of bounds
                for (int i = 0; i < moveOptions.Length; i++)
                {
                    // Already eliminated, skip
                    if (moveOptions[i] == "-")
                        continue;
                    
                    Vector2Int a = Project(moveOptions[i], current.Position, hex);
                    
                    if (a.x < 0 || a.y < 0 || map.GetLength(0) <= a.x || map.GetLength(1) <= a.y)
                        moveOptions[i] = "-";
                }
                
                // Remove movement options that lead to a Node that has already been visited
                for (int i = 0; i < moveOptions.Length; i++)
                {
                    // Already eliminated, skip
                    if (moveOptions[i] == "-")
                        continue;
                    
                    Vector2Int a = Project(moveOptions[i], current.Position, hex);
                    
                    if (allVisited.Any(c => c.Position == a))
                        moveOptions[i] = "-";
                }
                
                // No move options left, gonna backtrace
                if (moveOptions.All(c => c == "-"))
                {
                    // Can't backtrace any further, maze finished
                    if (path.Count == 1)
                        break;
                    
                    // Move to previous node
                    path.RemoveAt(path.Count - 1);
                    continue;
                }

                
                // Put remaining options into a list
                List<string> remaining = moveOptions.Where(c => c != "-").ToList();
                // Choose random movement option out of remaining movement options
                string chosen = remaining[UnityEngine.Random.Range(0, remaining.Count)];
                
                
                // Get the position of the next node
                Vector2Int pos = Project(chosen, current.Position, hex);
                
                // Add the next node to the path
                path.Add(map[pos.x, pos.y]);
                allVisited.Add(path.Last());
                
                // Get the index of the chosen option, to mark this direction in the current node's Went and Openings bool arrays
                int index = moveOptions.ToList().IndexOf(chosen);
                current.Went[index] = true;
                current.Openings[index] = true;
                
                // Get the index of the opposite of the chosen option and mark that direction in the next node's Openings bool array
                int opposite = index + (hex ? 3 : 2);
                if (opposite > (hex ? 5 : 3))
                    opposite -= hex ? 6 : 4;
                path.Last().Openings[opposite] = true;
                
                
                
                // Check if at finish (endpoint),            for the first time
                if (path.Last().Position == runParams.End && SpawnedCompletionTrails.Count == 0)
                {
                    Transform trail = Instantiate(Resources.Load<GameObject>("CompletionTrail"), new Vector3(path[0].Position.x, path[0].Position.y), Quaternion.identity).transform;
                    SpawnedCompletionTrails.Add(trail.gameObject);
                    
                    // Run the trialRenderer through each node in the path's position
                    foreach (Node n in path)
                    {
                        trail.position = hex ?
                            Rendering.GetHexWorldPos(n.Position) : // if hexagonal, center by using Tile-map's position conversion thing
                            new Vector3(n.Position.x + 0.5f, n.Position.y + 0.5f); // if rectangular, center by simply adding 0.5, 0.5
                        
                        yield return new WaitForSeconds(waitTime);
                    }
                }
                
                map.RenderTile(current.Position);
                map.RenderTile(path.Last().Position);
                yield return new WaitForSeconds(waitTime);
            }

            // Maze finished generating
            
            startedRunCoroutine = null;
            completionCallback?.Invoke();
        }
    }
}