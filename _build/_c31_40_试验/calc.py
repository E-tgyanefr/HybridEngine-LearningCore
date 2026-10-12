import random, sys
INF = 10**9
def bf(n, s, edges, passes):
    d = [INF]*n; d[s] = 0
    for _ in range(passes):
        for (u,v,w) in edges:
            if d[u] != INF and d[u] + w < d[v]:
                d[v] = d[u] + w
    return d

print("== 40: 5 点 7 边, 5 种输入顺序 ==")
E = [(0,1,4),(0,2,1),(2,1,1),(1,3,4),(2,3,5),(3,4,1),(1,4,9)]
n, s = 5, 0
d = bf(n, s, E, n-1)
print("  正确（松 n-1=4 遍）:", d)
for k in range(6):
    r = E[:]; random.seed(k); random.shuffle(r)
    print(f"  seed{k} 松4遍 -> {bf(n,s,r,n-1)}   松1遍 -> {bf(n,s,r,1)}")

print()
print("== 40: 另一张图（负边） ==")
E2 = [(0,1,2),(1,2,1),(0,2,5),(2,3,-3),(3,4,1),(1,4,10)]
print("  松4遍:", bf(5,0,E2,4), " 松1遍:", bf(5,0,E2,1))

print()
print("== 34: 4x4 迷宫 BFS 手算核对 ==")
maze=[[0,1,0,0],[0,0,1,0],[1,0,0,0],[1,1,1,0]]
from collections import deque
q=deque([(0,0,0)]); seen={(0,0)}
while q:
    r,c,st=q.popleft()
    if (r,c)==(3,3): print("  BFS 最少步数 =", st); break
    for dr,dc in ((-1,0),(1,0),(0,-1),(0,1)):
        nr,nc=r+dr,c+dc
        if 0<=nr<4 and 0<=nc<4 and maze[nr][nc]==0 and (nr,nc) not in seen:
            seen.add((nr,nc)); q.append((nr,nc,st+1))

print()
print("== 35: 打家劫舍 / 背包 手算核对 ==")
def rob(a):
    dp=[0]*(len(a)+1); dp[1]=a[0]
    for i in range(2,len(a)+1): dp[i]=max(dp[i-1],dp[i-2]+a[i-1])
    return dp[len(a)]
def knap(W,V,cap):
    dp=[0]*(cap+1)
    for i in range(len(W)):
        for j in range(cap,W[i]-1,-1): dp[j]=max(dp[j],dp[j-W[i]]+V[i])
    return dp[cap]
def knap_forward(W,V,cap):
    dp=[0]*(cap+1)
    for i in range(len(W)):
        for j in range(W[i],cap+1): dp[j]=max(dp[j],dp[j-W[i]]+V[i])
    return dp[cap]
print("  rob [2,7,9,3,1] =", rob([2,7,9,3,1]), " [5,1,1,5] =", rob([5,1,1,5]), " [1] =", rob([1]), " [4,4,4] =", rob([4,4,4]))
print("  knap W2,3 V3,4 cap8 =", knap([2,3],[3,4],8), " cap3 =", knap([2,3],[3,4],3), " cap1 =", knap([2,3],[3,4],1), " 正着走(错) =", knap_forward([2,3],[3,4],8))
print("  knap W2,3,4,5 V3,4,5,8 cap8 =", knap([2,3,4,5],[3,4,5,8],8))
print("  knap W1,2,3 V6,10,12 cap5 =", knap([1,2,3],[6,10,12],5))

print()
print("== 35: 爬楼梯 ==")
def climb(n):
    if n<=0: return 0
    if n==1: return 1
    dp=[0]*(n+1); dp[1]=1; dp[2]=2
    for i in range(3,n+1): dp[i]=dp[i-1]+dp[i-2]
    return dp[n]
for n in (1,2,3,5,10,45): print(f"  climb({n}) = {climb(n)}")

print()
print("== 39: 杨辉三角核对 ==")
N=20; C=[[0]*(N+1) for _ in range(N+1)]; C[0][0]=1
for n in range(1,N+1):
    C[n][0]=1
    for k in range(1,n+1): C[n][k]=C[n-1][k-1]+C[n-1][k]
print("  C(5,2)=",C[5][2]," C(6,3)=",C[6][3]," C(10,4)=",C[10][4]," C(20,10)=",C[20][10])
print("  前5行:", [[C[n][k] for k in range(n+1)] for n in range(5)])

print()
print("== 37: 路径数 / LCS 核对 ==")
def paths(r,c):
    dp=[[1]*c for _ in range(r)]
    for i in range(1,r):
        for j in range(1,c): dp[i][j]=dp[i-1][j]+dp[i][j-1]
    return dp[r-1][c-1]
def lcs(a,b):
    dp=[[0]*(len(b)+1) for _ in range(len(a)+1)]
    for i in range(1,len(a)+1):
        for j in range(1,len(b)+1):
            dp[i][j]=dp[i-1][j-1]+1 if a[i-1]==b[j-1] else max(dp[i-1][j],dp[i][j-1])
    return dp[len(a)][len(b)]
for rc in ((3,7),(1,5),(4,4),(2,2),(1,1),(5,5)): print(f"  paths{rc} = {paths(*rc)}")
for a,b in (("12345","135"),("abcde","ace"),("abc","abc"),("abc","xyz"),("",""),("a","a")): print(f'  lcs("{a}","{b}") = {lcs(a,b)}')

print()
print("== 36: printf %f 六位小数 ==")
import math
for label,val in (("sqrt(16)",math.sqrt(16)),("sqrt(2)",math.sqrt(2)),("pow(2,10)",2.0**10),("pow(3,3)",3.0**3),("fabs(-2.5)",2.5),("floor(2.5)",math.floor(2.5)),("floor(-2.5)",math.floor(-2.5)),("ceil(2.1)",math.ceil(2.1))):
    print(f"  {label} = {val:.6f}")