from collections import deque
maze=[[0,0,1,0],[0,0,1,0],[0,0,0,0]]
R,C=3,4
q=deque([(0,0,0)]); seen={(0,0):None}; end=None
while q:
    r,c,st=q.popleft()
    if (r,c)==(R-1,C-1): end=(r,c); print("最短 =",st); break
    for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
        nr,nc=r+dr,c+dc
        if 0<=nr<R and 0<=nc<C and maze[nr][nc]==0 and (nr,nc) not in seen:
            seen[(nr,nc)]=(r,c); q.append((nr,nc,st+1))
path=[]; cur=end
while cur: path.append(cur); cur=seen[cur]
print("路径:", " → ".join(f"({r},{c})" for r,c in reversed(path)))