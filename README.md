## Countdown

An application that solves the numbers and letters games in the Uk Channel 4 program Countdown.

Countdown is a popular day time television game show. One of the games is a number puzzle where a contestant picks 6 numbers and then has to build an equation from them that equals a randomly generated target during a 30 second countdown. If you haven't seen the program the rules are available [here](https://en.wikipedia.org/wiki/Countdown_(game_show)#Letters_round)
This puzzle is an ideal candidate to be solved by a computer as it won't ever forget which numbers it's already used and it will always find the really obvious solution that's staring you right in the face but just cannot see.

#### The UI

With no multiplayer capability or localizable UI it's not up to commercial standards however it provides enough functionality to play the game and exercise the solver. It could be useful as a training aid.

<dl><dd><dl><dd><dl><dd>
<img height="500" alt="numbers" src="https://github.com/user-attachments/assets/70d0a3c7-e512-48e4-a02b-890df79fdc54" />
</dd></dl></dd></dl></dd></dl>

The Choose button generates tiles and the target values.
The Start/Stop Timer button controls a 30 second timer.
The Solve button starts the `SolvingEngine`.

#### How the Solving Engine works
This uses a simple brute force approach where every possible equation is recursively evaluated. The equations are constructed in postfix form which removes the need for parentheses and this considerably simplifies the problem. A map is used to define how the postfix equations are constructed.
The model's `Solve()` method is shown below:
````c#
public static SolverResults Solve(int[] tiles, int target)
{
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
    SolverResults results = new SolverResults();

    for (int k = 2; k <= tiles.Length; k++)
    {
        Combinations<int> combinations = new Combinations<int>(tiles, k);

        foreach (List<int> combination in combinations)
        {
            Permutations<int>; permutations = new Permutations<int>(combination);

            Parallel.ForEach(permutations,
                            // the partitions local solver
                            () => new SolvingEngine(target, k),
                            // the parallel action
                            (permutation, loopState, solvingEngine) =>
                            {
                                solvingEngine.Solve(permutation);
                                return solvingEngine;
                            },
                            // local finally
                            (solvingEngine) => results.AggregateData(solvingEngine));
        }
    }

    stopWatch.Stop();
    results.Elapsed = stopWatch.Elapsed;

    return results;
}
````
As you can see, it loops choosing successive k combinations of the 6 tiles. For each combination it calculates all its permutations and for each permutation it evaluates all possible postfix equations in a parallel foreach loop. Each parallel partition has its own solving engine. The engines `Solve()` method runs the engine for the given permutation, working though each map entry.
````c#
public void Solve(List<int> permutation)
{
    foreach (List<int> mapEntry in map)
    {
        SolveRecursive(-1, mapEntry, 0, permutation, 0, 0);
    }
}
````
The solving engine `SolveRecursive()` method is the main work horse. The code is rather long winded due to functions being manually in lined, so I've shown the equivalent pseudo code:
````
SolveRecursive(recursion_depth)
{
    // get the stack for this recursion depth
    stack = stacks[recursion_depth];

    while (mapEntry > 0)   // push tiles onto the stack as per map
    {
        stack.push;
        --mapEntry;
    }

    left_digit = stack.pop;
    right_digit = stack.peek;
 
    foreach (operator in {*, +, -, /})
    {
        result = evaluate (left_digit operator right_digit);
 
        if (there are more map entries)
        {
            // set up stack for next recursion
            next_stack = stacks[recursion_depth + 1]
            copy stack to next_stack
            next_stack.poke = result ;
 
            SolveRecursive(recursion_depth + 1)
        }
        else
        {
            if (result == target)
            {
                record equation ;
                break ;
            }
 
            if (no targets found and result is close enough)
                record closest equation;
        }
    }
}
````
The method implements a sequence of pushing zero or more tiles onto the stack before executing an operator. It then recurses executing another sequence. Each recursive call gets its own copy of the current stack so that when the recursion unwinds, the caller can simply continue with the next operator. The recursion ends when the map is completed.
Although it is a linear function conceptually it can be thought of as a tree. Each sequence splits into 4 branches, one for each operator, and for each recursion every branch further splits into 4 more branches. This repeats until the end of the map is reached when the final results are obtained for the 4 operators. After the first equation is executed, subsequent equations are evaluated by executing only one operator rather than the complete equation. The same principal applies for each node in the tree as the recursion unwinds. Tree structures and recursion are a thing of beauty.

#### Combinations and Permutations

The Combinations and Permutations collection classes are implemented using the common pattern where the class's enumerator provides the next combination or permutation on demand. To make the code reusable I've written them using generics.
Both classes use Donald Knuth documented algorithms to generate the data in lexicographic order. I think I can safely assume they are good solutions. The permutation algorithm is actually a port of a C++ standard template library function `std::next_permutation()`.
The combination algorithm doesn't produce duplicates. In addition, I've modified the algorithm to filter out duplicate combinations produced when the source collection itself contains duplicates.

#### Postfix Map

Postfix equations are built up of a repeating sequence of pushing digits on to a stack followed by executing operators. All equations start by pushing 2 digits followed by executing 0 or 1 operator, then another digit followed by 0 to 2 operators etc. There is always be one less operator than digits and equations always end with an operator.
Consider the case for 4 tiles, there will be 5 map sub entries, one for each possible equation:

<dl><dd><dl><dd><dl><dd>
<img width="600" alt="map entries" src="https://github.com/user-attachments/assets/02de996f-f47f-449c-ba44-77d6f0344c45" />
</dd></dl></dd></dl></dd></dl>

In the first column the $\large\color{#f00}{\texttt{o}}$ represents all the possible positions of operators within the equation. The code generates column two, a count of the operators in that position. It then converts these counts into a map entries where numbers greater than zero means push that number of digits onto the stack and zeros indicates that operators should be executed.

#### So how many equations are there?

It's the sum of the number of equations for each number of tiles k. If there are no duplicate tiles, then this is straight forward.

<dl><dd><dl><dd><dl><dd>
<img width="500" alt="equation" src="https://github.com/user-attachments/assets/9fba7182-e317-4b97-a206-6fbe1dfdf575" />
</dd></dl></dd></dl></dd></dl>

The last term is the product of the operators. So we have:

<dl><dd><dl><dd><dl><dd>
<img height="100" alt="equation results" src="https://github.com/user-attachments/assets/f327cd52-e12e-41b1-a559-83c7b2563539" />
</dd></dl></dd></dl></dd></dl>

This gives a total of 33,665,400 equations. Note that k = 6 accounts for 92% of the total.

However, according to the rules of the game, there can be one or two pairs of duplicate tiles. The problem then starts with calculating the number of combinations. Google found [this](https://web.archive.org/web/20200227130859/http://mathforum.org/library/drmath/view/56197.html) elegantly explained solution. Of course, some of the combinations produced would then also contain duplicates which in turn affects the number of permutations. I've left that problem for the mathematicians and just put some instrumentation into the code. The results are:

<dl><dd><dl><dd><dl><dd>
<img height="150" alt="totals" src="https://github.com/user-attachments/assets/4b92784b-513f-4867-b3f7-063c49f55ef9" />
</dd></dl></dd></dl></dd></dl>

In practice, the number of equations fully evaluated will be considerably smaller due to the positive integer arithmetic rules of the game. If an equation fails one of the rules, it is discarded as soon as it fails. In addition, I also discard any equation where an identity operation is performed, that is divide or multiple by one, since it will be a duplicate equation.
I also filter out duplicate equations by detecting if the operator is acting on tile values directly rather than any intermediately derived value. If the operator is commutative, that is addition and multiplication; I discard the equation if the left digit is greater than the right. The final filter is that multiplication is the first operator evaluated. If the equation is complete and the result is less than the target, then the other operators are skipped. Ultimately, it will be dependent on the value of the tiles chosen and the target as to how many equations are evaluated fully. For typical sets of tiles and targets:

<dl><dd><dl><dd><dl><dd>
<img height="110" alt="timings" src="https://github.com/user-attachments/assets/767da17f-4ca7-435c-82ea-e46d0ec6510f" />
</dd></dl></dd></dl></dd></dl>

The solver was run on a Dell Inspiron 5578 with an I7-7500 dual core processor (2.7 GHz) and 16 GB of memory running windows 10. Note that the timings were taken when the solver was started via the keyboard. Clicking took approximately 20ms longer, presumably due to an additional background process run by the system (it turns out that clicking triggered a garbage collection cycle).

#### Letters Game

In version 3.0 I've added the letters and conundrum games. I'd always intended too, but the problem was finding suitable word lists. Then I came across SOWPODS scrabble word lists. These don't contain proper nouns, hyphenated words, spelling errors etc. but unfortunately do include mass nouns.
To implement the game I preprocess the word list into a dictionary containing lists of anagrams. Once in anagram form the order of the letters within the words isn't relevant, so I use a sorted list of the letters as the dictionary key. The combination enumerator produces lexicographical combinations so to solve the puzzle it is simply a case feeding the users chosen letters into the enumerator and using its output as a key for the dictionary. The word list is compressed by a separate project within the solution and then added to the app as an embedded resource. The UI is straight forward:

<dl><dd><dl><dd><dl><dd>
<img height="500" alt="letters" src="https://github.com/user-attachments/assets/745f62cb-1690-40bf-a472-2d7590c382c8" /> 
</dd></dl></dd></dl></dd></dl>

You can either type letters or click the Vowel or Consonant buttons. To see if your word is valid you can type it in to the auto suggestion text box above the list. It uses a spell check algorithm to suggest matches.

#### Conundrum Game

This is just a subset of the letters game using 9 letter words which have only one solution. The choose button will always generate a valid conundrum. If you type in letters then the solve button will only be enabled if there is a valid single solution.

<dl><dd><dl><dd><dl><dd>
<img height="490" alt="conundrum" src="https://github.com/user-attachments/assets/2b7b3d80-7448-4765-a9bc-47b1ac824d8d" />
</dd></dl></dd></dl></dd></dl>

