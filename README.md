# Sampling Based Method
### [2025 2학기 로봇알고리즘 과제4]

### 개발 기간
> 2025.11.06 ~ 2025.11.13

### 개발 환경
> Unity 6000.2.1f1<br>
> Templates : Universal 3D<br>
> RTX4050 Laptop<br>

### 설명
+ 동기
  + 로봇알고리즘 수업 과제
+ 기획
  + 로봇 소개
    + 외형 <br>
    <img width="360" height="334" alt="outline" src="https://github.com/user-attachments/assets/5fd3564a-f6f2-495a-89f1-0be021ab29b0" />
    
    + 충돌 탐지 영역 <br>
    <img width="369" height="334" alt="collition-detection-area" src="https://github.com/user-attachments/assets/91fa792f-a850-4dad-8be1-792f5dfd6c62" />
    
    + 결과 <br>
    <img width="366" height="334" alt="result" src="https://github.com/user-attachments/assets/2f9fd9b8-fbcb-47e1-91da-0b405e39fe3f" />
    
  <br>
  
  + PRM (Probabilistic Roadmap Method)
    + Configuration Space $q = [x, y, z, \theta_x, \theta_y, \theta_z]$에서 무작위로 5,000개의 샘플 생성 (예 : $q = [0.52, 0.64, 1.15, 5.77, 84.21, 128.03]$)
    + K-NN 알고리즘을 이용하여 각 샘플 간 간선 연결 수행
      + 이때 거리계산에서 각도($\theta_x, \theta_y, \theta_z$)의 경우 직교좌표로 변환 후 수행
      + 즉, $q = [x, y, z, cos(\theta_x), sin(\theta_x), cos(\theta_y), sin(\theta_y), cos(\theta_z), sin(\theta_z)]$로 변환하여 유클리드 거리 계산 수행
      + K = 3
      + 샘플 간 간선 연결 시 Local Planning(샘플 사이에 장애물 존재 여부 체크) 수행
    + K-NN 알고리즘으로 그래프 생성 후, 다익스트라 알고리즘을 사용하여 최단거리 계산
      + 다익스트라 알고리즘 중, 거리계산 또한 위와 동일하게 수행
    + 회전 수행 <br>
    ![equation](https://latex.codecogs.com/png.image?%5Cinline%20%5Cdpi%7B110%7D%5Cbg%7Bwhite%7D%5C%5CP=%5Cbegin%7Bbmatrix%7Dp_%7Bx_%7B1%7D%7D&&p_%7Bx_%7B16%7D%7D%5C%5Cp_%7By_%7B1%7D%7D&%5Ccdots&p_%7By_%7B16%7D%7D%5C%5Cp_%7Bz_%7B1%7D%7D&&p_%7Bz_%7B16%7D%7D%5C%5C%5Cend%7Bbmatrix%7D%5C%5C%5C%5CR_%7Bx%7D=%5Cbegin%7Bbmatrix%7D1&0&0%5C%5C0&cos(%5Ctheta_%7Bx%7D)&-sin(%5Ctheta_%7Bx%7D)%5C%5C0&sin(%5Ctheta_%7Bx%7D)&cos(%5Ctheta_%7Bx%7D)%5C%5C%5Cend%7Bbmatrix%7D,%5C,R_%7By%7D=%5Cbegin%7Bbmatrix%7Dcos(%5Ctheta_%7By%7D)&0&-sin(%5Ctheta_%7By%7D)%5C%5C0&1&0%5C%5Csin(%5Ctheta_%7By%7D)&0&cos(%5Ctheta_%7By%7D)%5C%5C%5Cend%7Bbmatrix%7D,%5C,R_%7Bz%7D=%5Cbegin%7Bbmatrix%7Dcos(%5Ctheta_%7Bz%7D)&-sin(%5Ctheta_%7Bz%7D)&0%5C%5Csin(%5Ctheta_%7Bz%7D)&cos(%5Ctheta_%7Bz%7D)&0%5C%5C0&0&1%5C%5C%5Cend%7Bbmatrix%7D%5C%5C%5C%5CH=R_%7By%7D%5Ccdot%20R_%7Bx%7D%5Ccdot%20R_%7Bz%7D%5C%5C%5C%5CP_%7B%5Ctext%7Brotated%7D%7D=H%5Ccdot%20P=%5Cbegin%7Bbmatrix%7Dp'_%7Bx_%7B1%7D%7D&&p'_%7Bx_%7B16%7D%7D%5C%5Cp'_%7By_%7B1%7D%7D&%5Ccdots&p'_%7By_%7B16%7D%7D%5C%5Cp'_%7Bz_%7B1%7D%7D&&p'_%7Bz_%7B16%7D%7D%5C%5C%5Cend%7Bbmatrix%7D%5C%5C) <br><br>
  
  + RRT (Rapidly-exploring Random Tree)
    + Configuration Space $q = [x, y, z, \theta_x, \theta_y, \theta_z]$에서 무작위 샘플 $q_{rand}$ 추출
    + $q_{rand}$와 가장 가까운 샘플 point $q_{near}$를 탐색
      + 두 벡터 간 거리는 PRM과 동일하게 각도 성분을 직교좌표로 변환 후 계산
    + $q_{near}$에서 $q_{rand}$방향으로 $\gamma$만큼 이동한 지점 $q_{new}$와 $q_{near}$사이에 장애물 존재하는지 검사 후, 장애물이 없는 경우에 $q_{new}$ 추가
      + $\gamma = 0.01$
    + $q_{new}$와 목표지점 간 거리가 $\varepsilon$ 이하가 될 때까지 위 세 단계를 반복 수행
      + $\varepsilon = 1.0$
      + 반복 횟수가 20,000번 초과 시, 경로 미존재로 판단하여 종료함
    + 트리 생성 완료 후, 부모 노드로 계속 거슬러 이동하며 경로 탐색함

### PRM 실행결과

https://github.com/user-attachments/assets/65125bc2-cdf1-4d90-a28f-1254252829b3

### RRT 실행결과

https://github.com/user-attachments/assets/65f8fef4-b391-454a-a4f6-8689c9fd3448

<br>
