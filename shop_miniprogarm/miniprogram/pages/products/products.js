const API_BASE_URL = 'https://localhost:7024/api'; // API基础路径

Page({
  data: {
    products: [],         // 商品列表 (ProductViewModel[])
    currentPage: 1,
    pageSize: 9,          // 每页加载数量，3x3网格
    totalPages: 1,
    hasMoreData: true,    // 是否还有更多数据可加载
    loading: true,        // 初始加载状态
    isLoadingMore: false, // 是否正在加载更多
    networkError: false,  // 网络错误标志
    scrollViewHeight: 0,  // 滚动区域高度
    showBackToTop: false, // 是否显示回到顶部按钮
    scrollTop: 0          // 记录滚动位置，用于回到顶部
  },

  onLoad: function (options) {
    this.setScrollViewHeight();
    this.loadProducts(true); // 初始加载，传入true表示重置数据
  },

  setScrollViewHeight() {
    // 获取屏幕高度，减去可能的顶部导航和底部tabBar高度
    // 这个值需要根据您的实际布局精确调整
    const systemInfo = wx.getSystemInfoSync();
    const windowHeight = systemInfo.windowHeight;
    // 假设没有自定义导航栏，底部有标准tabBar约50px高，顶部可能有一些padding
    // 如果是自定义导航栏，需要获取其高度
    let searchBarHeight = 0; // 如果有搜索栏，需要减去其高度
    // const query = wx.createSelectorQuery();
    // query.select('.search-bar-container').boundingClientRect(rect => {
    //   if (rect) searchBarHeight = rect.height;
    // }).exec();

    this.setData({
      scrollViewHeight: windowHeight - searchBarHeight // 减去搜索栏高度（如果显示）
    });
  },

  loadProducts(isRefresh = false) {
    if (isRefresh) {
      this.setData({
        products: [],
        currentPage: 1,
        hasMoreData: true,
        loading: true,
        networkError: false
      });
    }

    if (!this.data.hasMoreData || this.data.isLoadingMore) {
      if(!isRefresh) this.setData({loading:false}); // 如果不是刷新，且没有更多数据或正在加载，则停止
      return;
    }

    this.setData({ isLoadingMore: true, loading: isRefresh }); // 如果是刷新，主loading也显示

    wx.request({
      url: `${API_BASE_URL}/Product/List`, // 【重要】这里应该换成用户端获取商品的接口
      method: 'GET',
      data: {
        pageIndex: this.data.currentPage,
        pageSize: this.data.pageSize,
        // isDeleted: false // 理想情况下，后端接口应默认不返回软删除商品给用户
      },
      // header: { 'Authorization': `Bearer ${token}` }, // 如果用户端接口需要登录才能看商品，则添加token
      success: (res) => {
        if (res.statusCode === 200) {
          const backendData = res.data; // PagedResponse<ProductViewModel>
          const newProducts = backendData.items || [];

          // 给每个商品增加一个 addingToCart 状态，用于按钮loading
          newProducts.forEach(p => p.addingToCart = false);

          this.setData({
            products: isRefresh ? newProducts : this.data.products.concat(newProducts),
            totalPages: backendData.totalPages || 1,
            hasMoreData: this.data.currentPage < (backendData.totalPages || 1),
            currentPage: this.data.currentPage + (newProducts.length > 0 ? 1 : 0), // 只有成功加载到数据才增加页码计数器
            networkError: false
          });
        } else {
          wx.showToast({ title: `加载商品失败: ${res.statusCode}`, icon: 'none' });
          this.setData({ networkError: true });
        }
      },
      fail: (err) => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
        console.error("loadProducts fail", err);
        this.setData({ networkError: true });
      },
      complete: () => {
        this.setData({ loading: false, isLoadingMore: false });
      }
    });
  },

  // 滚动到底部加载更多
  loadMoreProducts() {
    if (this.data.hasMoreData && !this.data.isLoadingMore) {
      this.loadProducts();
    }
  },

  // 下拉刷新
  onPullDownRefresh() {
    this.loadProducts(true); // true表示刷新，会重置数据
    wx.stopPullDownRefresh(); // 停止下拉刷新动画
  },

  // 跳转到商品详情页
  navigateToDetail(e) {
    const productId = e.currentTarget.dataset.id;
    wx.navigateTo({
      url: `/pages/productDetail/productDetail?id=${productId}` // 确保路径正确
    });
  },

  // 添加到购物车
  addToCart(e) {
    const product = e.currentTarget.dataset.product;
    if (product.addingToCart) return; // 防止重复点击

    const token = wx.getStorageSync('token');
    if (!token) {
      wx.showModal({
        title: '提示',
        content: '请先登录再操作',
        confirmText: '去登录',
        success: res => {
          if (res.confirm) wx.navigateTo({ url: '/pages/login/login' });
        }
      });
      return;
    }
    
    // 更新按钮状态为加载中
    this.setProductAddingState(product.id, true);

    wx.request({
      url: `${API_BASE_URL}/Cart/AddItemToCart`,
      method: 'POST',
      header: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' },
      data: {
        productId: product.id,
        quantity: 1 // 默认添加1件
      },
      success: (res) => {
        if (res.statusCode === 200 || res.statusCode === 201) {
          wx.showToast({ title: '添加成功', icon: 'success', duration: 1000 });
          // 更新购物车角标 (如果您的tabBar有购物车角标)
          // getApp().updateCartBadge(); 
        } else {
          let errorMsg = '添加失败';
          if (res.data && res.data.message) errorMsg = res.data.message;
          else if (res.data && res.data.title) errorMsg = res.data.title;
          wx.showToast({ title: errorMsg, icon: 'none' });
        }
      },
      fail: () => {
        wx.showToast({ title: '网络请求失败', icon: 'none' });
      },
      complete: () => {
        this.setProductAddingState(product.id, false); // 恢复按钮状态
      }
    });
  },

  setProductAddingState(productId, isAdding) {
    const products = this.data.products.map(p => {
      if (p.id === productId) {
        p.addingToCart = isAdding;
      }
      return p;
    });
    this.setData({ products });
  },

  // 页面滚动事件，用于显示/隐藏回到顶部按钮 (可选)
  onPageScroll(e) {
    const scrollTop = e.scrollTop;
    this.setData({
      showBackToTop: scrollTop > 300, // 滚动超过300px时显示
      scrollTop: scrollTop
    });
  },

  // 回到顶部 (可选)
  backToTop() {
    wx.pageScrollTo({
      scrollTop: 0,
      duration: 300
    });
  },
  
  // 搜索功能 (骨架，待实现)
  onSearch(e){
      const keyword = e.detail.value || ""; // 从input的confirm事件或button的tap事件获取
      wx.showToast({ title: `搜索: ${keyword}`, icon: 'none' });
      // 后续实现：
      // this.setData({ currentPage:1, products:[], hasMoreData: true, searchTerm: keyword });
      // this.loadProducts(true); // 重新加载并带上搜索参数
  }
});