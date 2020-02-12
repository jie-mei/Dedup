# Dedup
A demo dedupllication program leveraging external merge sort for processing salable text input. This program keeps the first occurrence of duplicated lines. The lines in the output file maintain the order in the input file. 

* **Q**: **What is the time complexity of this program?**  
**A**: The overall time complexity is O(NlgN), where N is the number of lines in the input file. More specifically, let n be the number of lines in the segmented file. The in-memory sort step takes O(N/n * lgn) time. The external merge step takes O(N * lg(N/n)) = O(NlgN) time. Other steps take O(N) time. 

* **Q**: **Why not use the hasing approach?**  
**A**: There are two drawbacks in the hashing approach: 1) Use line as the key is not scalable, but using the hash value of the string as key has collisions; 2) Use one centralized hash is not scalable, but distributed hash is much more compliated to implement.

## Algorithm

The deduplication procedure consists of the following steps ([implmentation](https://github.com/jie-mei/Dedup/blob/f7a9ed945da3fc217f1aeb0787f461e2d28c75d0/Dedup/Program.cs#L32)):

1. **Encode** - Encode the input lines with the line position in the original document. This step converts the input lines to Record objects. A Record is an abstract representation of an indexed line. The data is processed in stream in order to handle scalable input files.
2. **Segment** - Segment the indexed file into smaller segments, so that each segment can be sort in-memory.
3. **Sort** - Sort lines in each segmented files. Lines are sorted by their contents in a lexigraphic order.
4. **Merge** - Merge sorted segments into one file. The data is processed in stream.
5. **Dedup** - Dedup the content by removing the adjacent identical lines. The data is processed in stream.
6. **Segment** - Segment the deduped records into segments.
7. **Sort** - Sort lines in each segmented files by their position in the original document.
8. **Merge** - Merge the segments into one file.
9. **Decode** - Decode the content into a text file.

## Usage

**Compile the project**

```
cd Dedup
msbuild Dedup.csproj
```


**Generate test input**

The following command generates a 210000 lines input file `test.in.txt` in the current directory
```
python gen_test_input.py 10000 200000
```

**Run executable**

The following command processes `test.in.txt` and writes the deduplicated result in `test.out.txt`. Each intermediate segment file has no more than 10000 lines.
```
Dedup.exe test.in.txt test.out.txt 10000
```

## Tests

### Test Case 1

```bash
python gen_test_input.py 3 5
Dedup.exe test.in.txt test.out.txt 3
```

`test.in.txt` (1KB)
```
User 00000 logged in
User 00001 logged in
User 00002 logged in
User 00001 logged in
User 00002 logged in
User 00001 logged in
User 00000 logged in
User 00002 logged in
User 00000 logged out
User 00001 logged out
User 00002 logged out
```

`test.out.txt` (1KB)
```
User 00000 logged in
User 00001 logged in
User 00002 logged in
User 00000 logged out
User 00001 logged out
User 00002 logged out
```

### Test Case 2

```bash
python gen_test_input.py 100000 2000000
Dedup.exe test.in.txt test.out.txt 10000
````

`test.in.txt` (47,364KB)

`test.out.txt` (4,200KB)
```
User 00000 logged in
User 00001 logged in
User 00002 logged in
User 00003 logged in
User 00004 logged in
...
User 99995 logged out
User 99996 logged out
User 99997 logged out
User 99998 logged out
User 99999 logged out
```
