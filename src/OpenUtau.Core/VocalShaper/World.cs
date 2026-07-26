using System;

namespace VocalShaper.World {
	public static class World {
		public static (double, int) Synthesis(this VSVocoder vsVocoder,
			out double[] result,
			double[,] sp,
			double[,] ap,
			int fs,
			int fftSize,
			double[] f0,
			double[] tension,
			double[] breathiness,
			double[] voicing,
			double[] gender,
			double phase = 0,
			int noiseIndex = 0) {
			double f2i = (double)fftSize / fs;
			return vsVocoder.Synthesis(out result,
				(i, f) => {
					double fi = f * f2i;
					int floorIndex = (int)fi;
					int ceillingIndex = floorIndex + 1;
					return Math.Sqrt(VSMath.LineLerp(sp[i, floorIndex], sp[i, ceillingIndex], fi - floorIndex));
				},
				(i, f) => {
					double fi = f * f2i;
					int floorIndex = (int)fi;
					int ceillingIndex = floorIndex + 1;
					double ratio = fi - floorIndex;
					return Math.Sqrt(VSMath.LineLerp(sp[i, floorIndex], sp[i, ceillingIndex], ratio)) * VSMath.LineLerp(ap[i, floorIndex], ap[i, ceillingIndex], ratio);
				},
				f0,
				tension,
				breathiness,
				voicing,
				gender,
				phase,
				noiseIndex);
		}
	}
}
