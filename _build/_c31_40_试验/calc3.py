from collections import deque
def bfs(maze, R, C):
    q=deque([(0,0,0)]); seen={(0,0)}
    while q:
        r,c,st=q.popleft()
        if (r,c)==(R-1,C-1): return st
        for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
            nr,nc=r+dr,c+dc
            if 0<=nr<R and 0<=nc<C and maze[nr][nc]==0 and (nr,nc) not in seen:
                seen.add((nr,nc)); q.append((nr,nc,st+1))
    return -1
print("intro 3x3:", bfs([[0,0,0],[1,1,0],[0,0,0]],3,3))
print("坑 3x4  :", bfs([[0,0,1,0],[0,0,1,0],[0,0,0,0]],3,4))
print("作业 4x4:", bfs([[0,1,0,0],[0,0,1,0],[1,0,0,0],[1,1,1,0]],4,4))