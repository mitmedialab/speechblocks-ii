using System.Collections.Generic;
using System.Linq;

public class DScan
{
    public static string LookupDScan(string key, string scan) {
        if (null == scan) return null;
        return BinaryLookupDScan(key, scan, 0, scan.Length);
    }

    private static string BinaryLookupDScan(string key, string scan, int low, int high) {
        if (high - low < key.Length) return null;
        int mid = (low + high) / 2;
        int lineStart = scan.LastIndexOf('\n', mid) + 1;
        if (lineStart < low) return null;
        int lineEnd = scan.IndexOf('\n', mid + 1);
        if (lineEnd < 0) lineEnd = high;
        if (lineEnd > high) return null;
        int keyCompare = CompareToDScanLine(key, scan, lineStart);
        if (keyCompare < 0) {
            return BinaryLookupDScan(key, scan, low, lineStart);
        } else if (keyCompare > 0) {
            return BinaryLookupDScan(key, scan, lineEnd + 1, high);
        } else {
            int valueStart = lineStart + key.Length + 1;
            if (valueStart >= lineEnd) return "";
            return scan.Substring(valueStart, lineEnd - valueStart);
        }
    }

    private static int CompareToDScanLine(string key, string scan, int lineStart) {
        for (int i = 0; i < key.Length; ++i) {
            char scanchar = scan[lineStart + i];
            if (' ' == scanchar) { return 1; }
            char keychar = key[i];
            int charcmp = keychar.CompareTo(scanchar);
            if (0 != charcmp) return charcmp;
        }
        if (' ' == scan[lineStart + key.Length] || '\n' == scan[lineStart + key.Length]) return 0;
        return -1;
    }
}