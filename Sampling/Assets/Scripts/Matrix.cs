using System.Collections.Generic;
using System.Collections;

public class Matrix {

	public float[,] matrix;

	public Matrix(int m, int n) {
		this.matrix = new float[m, n];
	}

	public Matrix(float[,] m) {
		//this.mat = new float[m.GetLength(0), m.GetLength(1)];
		//for (int i = 0; i < m.GetLength(0); i++) {
		//	for (int j = 0; j < m.GetLength(1); j++) {
		//		this.mat[i, j] = m[i, j];
		//	}
		//}
		this.matrix = m;
	}

	public Matrix T() {
		Matrix transposed = new Matrix(this.matrix.GetLength(1), this.matrix.GetLength(0));
		for (int i = 0; i < this.matrix.GetLength(0); i++)
		{
			for (int j = 0; j < this.matrix.GetLength(1); j++)
			{
				transposed.matrix[j, i] = this.matrix[i, j];
			}
		}

		return transposed;
	}

	public Matrix Add(Matrix m) {
		if (this.matrix.GetLength(0) != m.matrix.GetLength(0) || this.matrix.GetLength(1) != m.matrix.GetLength(1)) {
			throw new System.Exception("the dimensions of the two matrices do not match.");
		}

		Matrix added = new Matrix(this.matrix.GetLength(0), this.matrix.GetLength(1));
		for (int i = 0; i < this.matrix.GetLength(0); i++) {
			for (int j = 0; j < this.matrix.GetLength(1); j++) {
				added.matrix[i, j] = this.matrix[i, j] + m.matrix[i, j];
			}
		}

		return added;
	}

	public Matrix Subtract(Matrix m) {
		Matrix subtracted = m.Multiply(-1.0f);
		subtracted = subtracted.Add(m);
		return subtracted;
	}

	public Matrix Multiply(Matrix m) {
		Matrix multipled = new Matrix(this.matrix.GetLength(0), m.matrix.GetLength(1));

		if (this.matrix.GetLength(1) != m.matrix.GetLength(0))
		{
			throw new System.Exception("the dimensions of the two matrices do not match.");
		}

		for (int i = 0; i < this.matrix.GetLength(0); i++)
		{
			for (int j = 0; j < m.matrix.GetLength(1); j++)
			{
				float total = 0.0f;
				for (int k = 0; k < this.matrix.GetLength(1); k++)
				{
					total += this.matrix[i, k] * m.matrix[k, j];
				}
				multipled.matrix[i, j] = total;
			}
		}
		return multipled;
	}

	public Matrix Multiply(float c) {
		Matrix multiplied = new Matrix(this.matrix.GetLength(0), this.matrix.GetLength(1));

		for (int i = 0; i < this.matrix.GetLength(0); i++) {
			for (int j = 0; j < this.matrix.GetLength(1); j++) {
				multiplied.matrix[i, j] = this.matrix[i, j] * c;
			}
		}

		return multiplied;
	}

	override public string ToString() {
		string result = "";
		for (int i = 0; i < this.matrix.GetLength(0); i++) {
			for (int j = 0; j < this.matrix.GetLength(1); j++) {
				result += (" " + this.matrix[i, j] + " ");
			}
			result += "\n";
		}
		return result;
	}
}
