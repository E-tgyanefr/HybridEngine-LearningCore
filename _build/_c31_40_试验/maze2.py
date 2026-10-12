from collections import deque
def real(maze,R,C):
    q=deque([(0,0,0)]); seen={(0,0)}
    while q:
        r,c,st=q.popleft()
        if (r,c)==(R-1,C-1): return st
        for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
            nr,nc=r+dr,c+dc
            if 0<=nr<R and 0<=nc<C and maze[nr][nc]==0 and (nr,nc) not in seen:
                seen.add((nr,nc)); q.append((nr,nc,st+1))
    return -1
def stack_pop_mark(maze,R,C):     # 出栈才标记（常见错法）
    st=[(0,0,0)]; seen=set()
    while st:
        r,c,s=st.pop()
        if (r,c) in seen: continue
        seen.add((r,c))
        if (r,c)==(R-1,C-1): return s
        for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
            nr,nc=r+dr,c+dc
            if 0<=nr<R and 0<=nc<C and maze[nr][nc]==0 and (nr,nc) not in seen:
                st.append((nr,nc,s+1))
    return -1
cells=[(r,c) for r in range(4) for c in range(4) if (r,c) not in ((0,0),(3,3))]
hits=[]
for m in range(1<<14):
    g=[[0]*4 for _ in range(4)]
    for i,(r,c) in enumerate(cells): g[r][c]=(m>>i)&1
    if g[0][1] or g[1][0]: continue
    a=real(g,4,4); b=stack_pop_mark(g,4,4)
    if a!=-1 and b!=-1 and a!=b: hits.append((a,b,[[g[r][c] for c in range(4)] for r in range(4)]))
hits.sort(key=lambda x:(x[0],x[1]))
print("出栈才标记的假 BFS：找到", len(hits), "个会答错的 4x4 迷宫")
for a,b,g in hits[:4]:
    print("  真 BFS=%d 步，出栈才标记会答 %d 步" % (a,b))
    for row in g: print("        " + " ".join(map(str,row)))
    print()