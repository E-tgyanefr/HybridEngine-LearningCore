from collections import deque
def real(maze,R=4,C=4):
    q=deque([(0,0,0)]); seen={(0,0)}
    while q:
        r,c,st=q.popleft()
        if (r,c)==(R-1,C-1): return st
        for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
            nr,nc=r+dr,c+dc
            if 0<=nr<R and 0<=nc<C and maze[nr][nc]==0 and (nr,nc) not in seen:
                seen.add((nr,nc)); q.append((nr,nc,st+1))
    return -1
def fake(maze,R=4,C=4):
    st=[(0,0,0)]; seen={(0,0)}
    while st:
        r,c,s=st.pop()
        if (r,c)==(R-1,C-1): return s
        for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
            nr,nc=r+dr,c+dc
            if 0<=nr<R and 0<=nc<C and maze[nr][nc]==0 and (nr,nc) not in seen:
                seen.add((nr,nc)); st.append((nr,nc,s+1))
    return -1
cells=[(r,c) for r in range(4) for c in range(4) if (r,c) not in ((0,0),(3,3))]
hits=[]
for m in range(1<<14):
    g=[[0]*4 for _ in range(4)]
    for i,(r,c) in enumerate(cells):
        g[r][c]=(m>>i)&1
    if g[0][1] or g[1][0]: continue
    a,b=real(g),fake(g)
    if a!=-1 and b!=-1 and a!=b:
        hits.append((a,b,[[g[r][c] for c in range(4)] for r in range(4)]))
hits.sort(key=lambda x:(x[0],x[1]))
print("共找到", len(hits), "个「假 BFS（用栈）会答错」的迷宫。最小的几个：")
seen_shape=set()
for a,b,g in hits:
    key=tuple(tuple(r) for r in g)
    if key in seen_shape: continue
    seen_shape.add(key)
    print("  真 BFS = %d 步，假 BFS（栈）会答 %d 步" % (a,b))
    for row in g: print("        " + " ".join(str(x) for x in row))
    print()
    if len(seen_shape)>=6: break