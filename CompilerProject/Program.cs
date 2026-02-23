Dictionary<string, List<string>> grammar = new();
Dictionary<string, HashSet<string>> first = new();
Dictionary<string, HashSet<string>> follow = new();
Dictionary<string, Dictionary<string, string>> parsingTable = new();

string startSymbol = "";

ReadGrammar();
ComputeFirst();
ComputeFollow();

// Always display FIRST and FOLLOW sets
DisplayFirst();
DisplayFollow();

// Always create and display parsing table
CreateParsingTable();
DisplayParsingTable();

if (!IsLL1Grammar())
{
    Console.WriteLine("\nThe grammar is NOT LL(1). Cannot proceed with parsing.");
    Console.WriteLine("Please review the FIRST/FOLLOW sets and parsing table above to understand the conflicts.");
    return;
}

Console.WriteLine("\nThe grammar is LL(1). Proceeding...");

// Parse input strings
while (true)
{
    Console.Write("\nEnter a string to parse (or 'exit' to quit): ");
    string input = Console.ReadLine()!;

    if (input.ToLower() == "exit")
        break;

    ParseString(input);
}


// ================= METHODS =================

void ReadGrammar()
{
    Console.Write("Enter number of productions: ");
    int n = int.Parse(Console.ReadLine()!);

    for (int i = 0; i < n; i++)
    {
        Console.Write("Production: ");
        string prod = Console.ReadLine()!;

        string lhs = prod.Split('=')[0];
        string rhs = prod.Split('=')[1];

        if (i == 0) startSymbol = lhs;

        grammar[lhs] = rhs.Split('/').ToList();
        first[lhs] = new HashSet<string>();
        follow[lhs] = new HashSet<string>();
        parsingTable[lhs] = new Dictionary<string, string>();
    }

    follow[startSymbol].Add("$");
}

// ================= FIRST =================
void ComputeFirst()
{
    bool changed;

    do
    {
        changed = false;
        foreach (var rule in grammar)
        {
            foreach (var prod in rule.Value)
            {
                // Handle epsilon production
                if (prod == "ε")
                {
                    changed |= first[rule.Key].Add("ε");
                    continue;
                }

                // Iterate through all symbols in the production
                bool allCanDeriveEpsilon = true;

                for (int i = 0; i < prod.Length; i++)
                {
                    string symbol = prod[i].ToString();

                    if (!char.IsUpper(prod[i]))
                    {
                        // Terminal: add to FIRST and stop
                        changed |= first[rule.Key].Add(symbol);
                        allCanDeriveEpsilon = false;
                        break;
                    }
                    else
                    {
                        // Non-terminal: add FIRST(symbol) - {ε} to FIRST(rule.Key)
                        foreach (var f in first[symbol])
                        {
                            if (f != "ε")
                                changed |= first[rule.Key].Add(f);
                        }

                        // If this non-terminal cannot derive ε, stop
                        if (!first[symbol].Contains("ε"))
                        {
                            allCanDeriveEpsilon = false;
                            break;
                        }
                    }
                }

                // If all symbols can derive ε, add ε to FIRST
                if (allCanDeriveEpsilon)
                {
                    changed |= first[rule.Key].Add("ε");
                }
            }
        }
    } while (changed);

}

// ================= FOLLOW =================
void ComputeFollow()
{
    bool changed;
    do
    {
        changed = false;
        foreach (var rule in grammar)
        {
            foreach (var prod in rule.Value)
            {
                // Skip epsilon productions
                if (prod == "ε")
                    continue;

                for (int i = 0; i < prod.Length; i++)
                {
                    if (char.IsUpper(prod[i]))
                    {
                        string B = prod[i].ToString();

                        // Look at all symbols after B
                        bool allFollowingCanDeriveEpsilon = true;

                        for (int j = i + 1; j < prod.Length; j++)
                        {
                            string next = prod[j].ToString();

                            if (!char.IsUpper(prod[j]))
                            {
                                // Terminal: add to FOLLOW(B) and stop
                                changed |= follow[B].Add(next);
                                allFollowingCanDeriveEpsilon = false;
                                break;
                            }
                            else
                            {
                                // Non-terminal: add FIRST(next) - {ε} to FOLLOW(B)
                                foreach (var f in first[next])
                                {
                                    if (f != "ε")
                                        changed |= follow[B].Add(f);
                                }

                                // If next cannot derive ε, stop
                                if (!first[next].Contains("ε"))
                                {
                                    allFollowingCanDeriveEpsilon = false;
                                    break;
                                }
                            }
                        }

                        // If B is at end OR all symbols after B can derive ε
                        // Add FOLLOW(A) to FOLLOW(B)
                        if (i + 1 >= prod.Length || allFollowingCanDeriveEpsilon)
                        {
                            foreach (var f in follow[rule.Key])
                                changed |= follow[B].Add(f);
                        }
                    }
                }
            }
        }
    } while (changed);
}

void DisplayFirst()
{
    Console.WriteLine("\nFIRST Sets:");
    foreach (var item in first)
        Console.WriteLine($"FIRST({item.Key}) = {{ {string.Join(", ", item.Value)} }}");
}

void DisplayFollow()
{
    Console.WriteLine("\nFOLLOW Sets:");
    foreach (var item in follow)
        Console.WriteLine($"FOLLOW({item.Key}) = {{ {string.Join(", ", item.Value)} }}");
}

// ================= LL(1) VALIDATION =================
bool IsLL1Grammar()
{
    Console.WriteLine("\n--- Checking LL(1) Conditions ---");
    bool isLL1 = true;

    foreach (var nonTerminal in grammar.Keys)
    {
        var productions = grammar[nonTerminal];

        // Condition 1: For each pair of productions, FIRST sets must be disjoint
        for (int i = 0; i < productions.Count; i++)
        {
            for (int j = i + 1; j < productions.Count; j++)
            {
                var first1 = GetFirstOfProduction(productions[i]);
                var first2 = GetFirstOfProduction(productions[j]);

                var intersection = first1.Intersect(first2).Where(x => x != "ε").ToList();

                if (intersection.Count > 0)
                {
                    Console.WriteLine($"  Conflict: FIRST({nonTerminal} -> {productions[i]}) ^ FIRST({nonTerminal} -> {productions[j]}) = {{ {string.Join(", ", intersection)} }}");
                    isLL1 = false;
                }
            }
        }

        // Condition 2: If ε is in FIRST(A), then FIRST(A) ∩ FOLLOW(A) must be empty
        if (first[nonTerminal].Contains("ε"))
        {
            var firstWithoutEpsilon = first[nonTerminal].Where(x => x != "ε");
            var intersection = firstWithoutEpsilon.Intersect(follow[nonTerminal]).ToList();

            if (intersection.Count > 0)
            {
                Console.WriteLine($"  Conflict: FIRST({nonTerminal}) ^ FOLLOW({nonTerminal}) = {{ {string.Join(", ", intersection)} }} (ε in FIRST)");
                isLL1 = false;
            }
        }
    }

    if (isLL1)
        Console.WriteLine("  All LL(1) conditions satisfied.");

    return isLL1;
}

HashSet<string> GetFirstOfProduction(string production)
{
    var result = new HashSet<string>();

    if (string.IsNullOrEmpty(production) || production == "ε")
    {
        result.Add("ε");
        return result;
    }

    bool allCanDeriveEpsilon = true;

    for (int i = 0; i < production.Length; i++)
    {
        string symbol = production[i].ToString();

        if (!char.IsUpper(production[i]))
        {
            // Terminal: add to result and stop
            result.Add(symbol);
            allCanDeriveEpsilon = false;
            break;
        }
        else
        {
            // Non-terminal: add FIRST(symbol) - {ε}
            foreach (var f in first[symbol])
            {
                if (f != "ε")
                    result.Add(f);
            }

            // If this non-terminal cannot derive ε, stop
            if (!first[symbol].Contains("ε"))
            {
                allCanDeriveEpsilon = false;
                break;
            }
        }
    }

    // If all symbols can derive ε, add ε
    if (allCanDeriveEpsilon)
    {
        result.Add("ε");
    }

    return result;
}

// ================= PARSING TABLE =================
void CreateParsingTable()
{
    foreach (var A in grammar.Keys)
    {
        foreach (var prod in grammar[A])
        {
            string symbol = prod[0].ToString();

            if (symbol != "ε" && !char.IsUpper(symbol[0]))
                parsingTable[A][symbol] = A + " -> " + prod;

            else if (char.IsUpper(symbol[0]))
                foreach (var f in first[symbol])
                    parsingTable[A][f] = A + " -> " + prod;

            if (prod == "ε")
                foreach (var f in follow[A])
                    parsingTable[A][f] = A + " -> ε";
        }
    }
}

// ================= DISPLAY TABLE =================
void DisplayParsingTable()
{
    var terminals = new HashSet<string>();

    foreach (var row in parsingTable)
        foreach (var col in row.Value.Keys)
            terminals.Add(col);

    var terminalList = terminals.ToList();
    
    // Calculate column widths
    int ntColWidth = 6;
    foreach (var nt in parsingTable.Keys)
        ntColWidth = Math.Max(ntColWidth, nt.Length + 2);
    
    int cellWidth = 12;
    foreach (var nt in parsingTable)
        foreach (var entry in nt.Value.Values)
            cellWidth = Math.Max(cellWidth, entry.Length + 2);

    Console.WriteLine("\nLL(1) PARSING TABLE\n");

    // Build separator line
    string separator = "+" + new string('-', ntColWidth);
    foreach (var t in terminalList)
        separator += "+" + new string('-', cellWidth);
    separator += "+";

    // Print top border
    Console.WriteLine(separator);

    // Print header row
    string header = "|" + "NT".PadRight(ntColWidth);
    foreach (var t in terminalList)
        header += "|" + t.PadRight(cellWidth);
    header += "|";
    Console.WriteLine(header);

    // Print header separator
    Console.WriteLine(separator);

    // Print each row
    foreach (var nt in parsingTable)
    {
        string row = "|" + nt.Key.PadRight(ntColWidth);
        foreach (var t in terminalList)
        {
            if (nt.Value.ContainsKey(t))
                row += "|" + nt.Value[t].PadRight(cellWidth);
            else
                row += "|" + "-".PadRight(cellWidth);
        }
        row += "|";
        Console.WriteLine(row);
        Console.WriteLine(separator);
    }
}

// ================= STRING PARSING =================
void ParseString(string input)
{
    Console.WriteLine($"\n--- Parsing: \"{input}\" ---\n");

    // Add end marker to input
    input += "$";

    Stack<string> stack = new();
    stack.Push("$");
    stack.Push(startSymbol);

    int pointer = 0;

    // For parse tree
    TreeNode root = new TreeNode(startSymbol);
    Stack<TreeNode> treeStack = new();
    treeStack.Push(root);

    Console.WriteLine($"{"Stack",-30} {"Input",-20} {"Action"}");
    Console.WriteLine(new string('-', 70));

    while (stack.Count > 0)
    {
        string stackContent = string.Join(" ", stack.Reverse());
        string remainingInput = input.Substring(pointer);
        string top = stack.Peek();
        string currentInput = input[pointer].ToString();

        if (top == "$" && currentInput == "$")
        {
            Console.WriteLine($"{stackContent,-30} {remainingInput,-20} Accept");
            Console.WriteLine("\nString ACCEPTED!");

            // Display parse tree
            Console.WriteLine("\n--- Parse Tree ---\n");
            PrintParseTree(root, "", true);
            return;
        }

        if (top == currentInput)
        {
            Console.WriteLine($"{stackContent,-30} {remainingInput,-20} Match '{top}'");
            stack.Pop();

            TreeNode matchedNode = treeStack.Pop();
            matchedNode.IsTerminal = true;

            pointer++;
        }
        else if (!char.IsUpper(top[0]) && top != "$")
        {
            Console.WriteLine($"{stackContent,-30} {remainingInput,-20} Error: Unexpected terminal '{top}'");
            Console.WriteLine("\nString REJECTED!");
            return;
        }
        else if (parsingTable.ContainsKey(top) && parsingTable[top].ContainsKey(currentInput))
        {
            string production = parsingTable[top][currentInput];
            string rhs = production.Split("->")[1].Trim();

            Console.WriteLine($"{stackContent,-30} {remainingInput,-20} Apply: {production}");

            stack.Pop();
            TreeNode parentNode = treeStack.Pop();

            if (rhs != "ε")
            {
                // Push symbols in reverse order
                List<TreeNode> children = new();
                for (int i = rhs.Length - 1; i >= 0; i--)
                {
                    string symbol = rhs[i].ToString();
                    stack.Push(symbol);

                    TreeNode childNode = new TreeNode(symbol);
                    children.Insert(0, childNode);
                    treeStack.Push(childNode);
                }
                parentNode.Children = children;
            }
            else
            {
                // Epsilon production
                TreeNode epsilonNode = new TreeNode("ε") { IsTerminal = true };
                parentNode.Children = new List<TreeNode> { epsilonNode };
            }
        }
        else
        {
            Console.WriteLine($"{stackContent,-30} {remainingInput,-20} Error: No production for [{top}, {currentInput}]");
            Console.WriteLine("\nString REJECTED!");
            return;
        }
    }

    Console.WriteLine("\nString REJECTED!");
}

// ================= PARSE TREE =================
void PrintParseTree(TreeNode node, string indent, bool isLast)
{
    Console.WriteLine("\n");
    PrintTreeVertical(node);
}

void PrintTreeVertical(TreeNode root)
{
    // Get all levels of the tree
    List<List<TreeNode>> levels = new List<List<TreeNode>>();
    List<List<int>> positions = new List<List<int>>();
    
    // BFS to collect nodes by level
    Queue<(TreeNode node, int pos, int level)> queue = new Queue<(TreeNode, int, int)>();
    queue.Enqueue((root, 40, 0));  // Start root at position 40
    
    Dictionary<TreeNode, int> nodePositions = new Dictionary<TreeNode, int>();
    Dictionary<TreeNode, TreeNode> parentMap = new Dictionary<TreeNode, TreeNode>();
    
    while (queue.Count > 0)
    {
        var (node, pos, level) = queue.Dequeue();
        
        while (levels.Count <= level)
        {
            levels.Add(new List<TreeNode>());
            positions.Add(new List<int>());
        }
        
        levels[level].Add(node);
        positions[level].Add(pos);
        nodePositions[node] = pos;
        
        int childCount = node.Children.Count;
        if (childCount > 0)
        {
            int spacing = Math.Max(4, 20 / (level + 1));
            int totalWidth = (childCount - 1) * spacing;
            int startPos = pos - totalWidth / 2;
            
            for (int i = 0; i < childCount; i++)
            {
                int childPos = startPos + i * spacing;
                queue.Enqueue((node.Children[i], childPos, level + 1));
                parentMap[node.Children[i]] = node;
            }
        }
    }
    
    // Print tree level by level
    for (int level = 0; level < levels.Count; level++)
    {
        // Print nodes
        char[] nodeLine = new char[80];
        for (int i = 0; i < 80; i++) nodeLine[i] = ' ';
        
        for (int i = 0; i < levels[level].Count; i++)
        {
            TreeNode node = levels[level][i];
            int pos = positions[level][i];
            string symbol = node.IsTerminal ? $"[{node.Symbol}]" : node.Symbol;
            
            int startIdx = Math.Max(0, pos - symbol.Length / 2);
            if (startIdx + symbol.Length > 80)
                startIdx = 80 - symbol.Length;
            
            for (int j = 0; j < symbol.Length && startIdx + j < 80; j++)
            {
                nodeLine[startIdx + j] = symbol[j];
            }
        }
        Console.WriteLine(new string(nodeLine).TrimEnd());
        
        // Print connecting lines to children (if not last level)
        if (level < levels.Count - 1)
        {
            char[] connectorLine = new char[80];
            char[] branchLine = new char[80];
            for (int i = 0; i < 80; i++)
            {
                connectorLine[i] = ' ';
                branchLine[i] = ' ';
            }
            
            for (int i = 0; i < levels[level].Count; i++)
            {
                TreeNode node = levels[level][i];
                int parentPos = positions[level][i];
                
                if (node.Children.Count > 0)
                {
                    // Draw vertical line from parent
                    if (parentPos >= 0 && parentPos < 80)
                        connectorLine[parentPos] = '|';
                    
                    // Get child positions
                    List<int> childPositions = new List<int>();
                    foreach (var child in node.Children)
                    {
                        if (nodePositions.ContainsKey(child))
                            childPositions.Add(nodePositions[child]);
                    }
                    
                    if (childPositions.Count == 1)
                    {
                        // Single child - just vertical line
                        int childPos = childPositions[0];
                        if (childPos >= 0 && childPos < 80)
                            branchLine[childPos] = '|';
                    }
                    else if (childPositions.Count > 1)
                    {
                        // Multiple children - draw horizontal branch
                        int minPos = childPositions.Min();
                        int maxPos = childPositions.Max();
                        
                        for (int p = minPos; p <= maxPos && p < 80; p++)
                        {
                            if (p >= 0)
                                branchLine[p] = '-';
                        }
                        
                        foreach (int childPos in childPositions)
                        {
                            if (childPos >= 0 && childPos < 80)
                                branchLine[childPos] = '+';
                        }
                    }
                }
            }
            
            Console.WriteLine(new string(connectorLine).TrimEnd());
            Console.WriteLine(new string(branchLine).TrimEnd());
        }
    }
}

// ================= TREE NODE CLASS =================
class TreeNode
{
    public string Symbol { get; set; }
    public List<TreeNode> Children { get; set; }
    public bool IsTerminal { get; set; }

    public TreeNode(string symbol)
    {
        Symbol = symbol;
        Children = new List<TreeNode>();
        IsTerminal = false;
    }
}
