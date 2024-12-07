using System;

class SimplexMethod
{
    private double[,] tableau; // The tableau
    private int numRows, numCols; // Rows and Columns in the tableau
    private double[] basics; //The basic values

    // Constructor initializes the tableau
    public SimplexMethod(double[,] tableau)
    {
        this.tableau = tableau;
        numRows = tableau.GetLength(0);
        numCols = tableau.GetLength(1);
        this.basics = new double[GetBasicElementsNumber(numCols)];
        InitBasic(this.basics);
    }

    private int GetBasicElementsNumber(int numCols)
    {
        return (numCols - 1) / 2;
    }

    private void InitBasic(double[] basics)
    {
        for(int i=0; i<basics.Length; i++)
        {
            basics[i] = -1;
        }
    }

    // Solve the linear program
    public void Solve()
    {
        while (true)
        {
            int pivotCol = FindPivotColumn();
            if (pivotCol == -1)
            {
                Console.WriteLine("Optimal solution found!");
                double[] solution = PrintSolutionAndGet();
                if (ExistNonIntegralSolution())
                {
                    DoGomory(solution);
                }
                return;
            }

            int pivotRow = FindPivotRow(pivotCol);
            if (pivotRow == -1)
            {
                Console.WriteLine("The problem is unbounded.");
                return;
            }

            Pivot(pivotRow, pivotCol);
            PrintTableau();
            basics[pivotRow] = pivotCol;
        }
    }

    private void DoGomory(double[] solution)
    {   
        Console.WriteLine("Gomory method");
        do
        {
            var xColumnIndex = FindXColumnIndexToAddGomoryConstraint(solution);
            var gomoryConstraintRow = GetGomoryConstraintRowIndex(this.basics, xColumnIndex);
            double[] gomoryConstraint = CreateGomoryConstraintFor(gomoryConstraintRow, this.tableau);
            this.tableau = AddGomoryConstraint(gomoryConstraint, this.tableau);
            PrintTableau();

            int gomoryPivotRow = this.numRows - 2;
            int gomoryPivotCol = FindGomoryPivotCol(this.tableau);
            this.basics = ExtendBasics(gomoryPivotCol, this.basics);
            Pivot(gomoryPivotRow, gomoryPivotCol);
            PrintTableau();


        } while (ExistNonIntegralSolution());
       
    }

    private double[] ExtendBasics(int gomoryPivotCol, double[] oldBasics)
    {
        double[] newBasics = new double[this.numRows-2];
        for(int row = 0; row < newBasics.Length-1; row++)
        {
            newBasics[row] = oldBasics[row];
        }
        newBasics[newBasics.Length - 1] = gomoryPivotCol;
        return newBasics;
    }

    //Find the gomory's pivot row 
    private int FindGomoryPivotCol(double[,] tableau)
    {
        int lastRowIndex = this.numRows - 1;
        int gomoryConstraintRowIndex = lastRowIndex - 1;
        int gomoryPivotCol = 0;
        double min = 0.0;

        for(int col = 0; col<this.numCols - 2; col++)
        {
            if (tableau[gomoryConstraintRowIndex, col] != 0)
            {
                double lastRowGomoryRowRapport = tableau[lastRowIndex, col] / tableau[gomoryConstraintRowIndex, col];
                if (lastRowGomoryRowRapport <= min)
                {
                    min = lastRowGomoryRowRapport;
                    gomoryPivotCol = col;
                }
            }
        }

        return gomoryPivotCol;
    }

    // Find the pivot column (most negative in the objective row)
    private int FindPivotColumn()
    {
        int pivotCol = -1;
        double mostNegative = 0;

        for (int col = 0; col < numCols - 1; col++)
        {
            if (tableau[numRows - 1, col] < mostNegative)
            {
                mostNegative = tableau[numRows - 1, col];
                pivotCol = col;
            }
        }

        return pivotCol;
    }

    // Find the pivot row (smallest ratio in the pivot column)
    private int FindPivotRow(int pivotCol)
    {
        int pivotRow = -1;
        double minRatio = double.PositiveInfinity;

        for (int row = 0; row < numRows - 1; row++)
        {
            if (tableau[row, pivotCol] > 0)
            {
                double ratio = tableau[row, numCols - 1] / tableau[row, pivotCol];
                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    pivotRow = row;
                }
            }
        }

        return pivotRow;
    }

    // Perform the pivot operation
    private void Pivot(int pivotRow, int pivotCol)
    {
        double pivotValue = tableau[pivotRow, pivotCol];

        // Normalize the pivot row
        for (int col = 0; col < numCols; col++)
            tableau[pivotRow, col] /= pivotValue;

        // Eliminate other rows
        for (int row = 0; row < numRows; row++)
        {
            if (row != pivotRow)
            {
                double factor = tableau[row, pivotCol];
                for (int col = 0; col < numCols; col++)
                    tableau[row, col] -= factor * tableau[pivotRow, col];
            }
        }
    }

    // Print the solution
    private double[] PrintSolutionAndGet()
    {
        Console.WriteLine("Solution:");
        double[] solution = new double[this.basics.Length];
        for(int i=0; i<this.basics.Length; i++)
        {
            var xCol = this.basics[i];

            if (xCol != -1)
            {
                solution[(int)xCol] = this.tableau[i, numCols - 1];
            }
        }

        for(int i=0; i<solution.Length; i++)
        {
            Console.WriteLine($"x{i + 1} = {solution[i]}");
        }

        return solution;
    }

    private int FindXColumnIndexToAddGomoryConstraint(double[] solution)
    {
        int xColumnIndex = 0;
        double fractionPart = GetFractionPartOf(solution[xColumnIndex]);
        for(int i=1; i<solution.Length; i++)
        {
            var currentFractionPart = GetFractionPartOf(solution[i]);
            if (currentFractionPart >= fractionPart)
            {
                xColumnIndex = i;
                fractionPart = currentFractionPart;
            }
        }

        return xColumnIndex;
    }

    private int GetGomoryConstraintRowIndex(double[] basics, int xColumnIndexConstraint)
    {
        for(int i=0; i<basics.Length; i++)
        {
            if (basics[i] == xColumnIndexConstraint)
            {
                return i;
            }
        }
        return 0;
    }

    private double[] CreateGomoryConstraintFor(int constraintRowIndex, double[,] tableau)
    {
        int currentColsNum = tableau.GetLength(1);
        int newColsNum = currentColsNum + 1;
        double[] gomoryConstraint = new double[newColsNum];
        int oldFreeElementIndex = currentColsNum - 1;
        int lastOldXIndex = currentColsNum - 2;
        int newAddedXIndex = lastOldXIndex + 1;
        int newFreeElementIndex = newAddedXIndex + 1;

        for(int col=0; col<=lastOldXIndex; col++)
        {
            double currentXFractionPart = GetQCoefficientOf(tableau[constraintRowIndex, col]);
            gomoryConstraint[col] = currentXFractionPart;
        }
        gomoryConstraint[newAddedXIndex] = 1;
        gomoryConstraint[newFreeElementIndex] = GetQCoefficientOf(tableau[constraintRowIndex, oldFreeElementIndex]);

        return gomoryConstraint;
    }

    private double[,] AddGomoryConstraint(double[] gomoryConstraint, double[,] oldTableau)
    {
        int oldRowsNum = oldTableau.GetLength(0);
        int oldColsNum = oldTableau.GetLength(1);
        int newRowsNum = oldRowsNum + 1;
        int newColsNum = oldColsNum + 1;
        double[,] tableauWithAddedGomoryConstraint = new double[newRowsNum, newColsNum];
        this.numRows = newRowsNum;
        this.numCols = newColsNum;

        int gomoryConstraintRowIndex = newRowsNum - 2;
        int gomoryConstraintColIndex = newColsNum - 2;
        int lastRowIndex = newRowsNum - 1;
        int lastColIndex = newColsNum - 1;

        for (int row = 0; row< oldRowsNum; row++)
        {
            for(int col = 0; col< oldColsNum; col++)
            {
                if (row == gomoryConstraintRowIndex)
                {
                    tableauWithAddedGomoryConstraint[row, col] = gomoryConstraint[col];
                }
                else if(col==gomoryConstraintColIndex)
                {
                    tableauWithAddedGomoryConstraint[row, col] = 0.0;
                }
                else
                {
                    tableauWithAddedGomoryConstraint[row, col] = oldTableau[row, col];
                }
                
            }
        }

        //Last new row : F's row
        for(int col = 0; col<newColsNum; col++)
        {
            if (col == gomoryConstraintColIndex)
            {
                tableauWithAddedGomoryConstraint[lastRowIndex, col] = 0.0;
            }
            else if (col == newColsNum - 1)
            {
                tableauWithAddedGomoryConstraint[lastRowIndex, col] = GetOpositeSignIfGreaterThanZero(oldTableau[lastRowIndex - 1, col - 1]);
            }
            else
            {
                tableauWithAddedGomoryConstraint[lastRowIndex, col] = GetOpositeSignIfGreaterThanZero(oldTableau[lastRowIndex - 1, col]);
            }
        }

        //Last new col: B's column
        for(int row = 0; row<oldRowsNum; row++)
        {
            if (row == gomoryConstraintRowIndex)
            {
                tableauWithAddedGomoryConstraint[row, lastColIndex] = gomoryConstraint[lastColIndex];
            }
            else
            {
                tableauWithAddedGomoryConstraint[row, lastColIndex] = oldTableau[row, lastColIndex - 1];
            }
        }
        

        return tableauWithAddedGomoryConstraint;
    }

    private double GetOpositeSignIfGreaterThanZero(double value) => value > 0 ? -1 * value : value;

    private double GetFractionPartOf(double value) => value - Math.Floor(value);

    private double GetQCoefficientOf(double x) => GetOpositeSignIfGreaterThanZero(GetFractionPartOf(x));
    private bool ExistNonIntegralSolution()
    {
        for(int i=0; i<numRows; i++)
        {
            double fractionalPart = this.tableau[i, numCols - 1] - Math.Floor(this.tableau[i, numCols - 1]);
            if (Math.Round(fractionalPart,5).CompareTo(0.00000) != 0)
            {
                return true;
            }
        }

        return false;
    }

    // Utility to display the tableau
    private void PrintTableau()
    {
        Console.WriteLine("\nCurrent Tableau:");
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
                Console.Write($"{tableau[row, col],8:F2} ");
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    // Entry point for testing
    public static void Main()
    {
        // Example: Maximize z = 3x1 + 2x2
        // Subject to:
        // x1 + 2x2 <= 4
        // 3x1 + 2x2 <= 6
        // x1, x2 >= 0
        double[,] tableau = {
            {  1,  2,  3,  1,  0,  0,35 },
            {  4,  3,  2,  0,  1,  0,45 },
            { 3, 1,  1,  0,  0,  1,40 },
            { -4, -5,  -6,  0,  0,  0,0 }// Objective row
        };

        SimplexMethod simplex = new SimplexMethod(tableau);
        simplex.Solve();
        
    }
}
