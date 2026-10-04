(() => {
  const form = document.querySelector('#login-form');
  const image = document.querySelector('#captcha-image');
  const error = document.querySelector('#login-error');

  async function refreshCaptcha() {
    const response = await fetch(`/api/auth/captcha?ts=${Date.now()}`, { cache: 'no-store' });
    const challenge = await response.json();
    image.src = challenge.imageData;
    image.dataset.id = challenge.id;
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    error.className = 'error';
    error.textContent = '正在登录...';
    const payload = {
      username: document.querySelector('#username').value,
      password: document.querySelector('#password').value,
      captchaId: image.dataset.id,
      captchaCode: document.querySelector('#captcha-code').value
    };
    try {
      const response = await fetch('/api/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload), credentials: 'same-origin' });
      const message = await response.text();
      if (response.ok) {
        error.className = 'success';
        error.textContent = message ? (JSON.parse(message).message || '登录成功') : '登录成功';
        window.setTimeout(() => { window.location.assign('/'); }, 500);
      } else {
        error.className = 'error';
        error.textContent = message || `登录失败（HTTP ${response.status}）`;
        await refreshCaptcha();
      }
    } catch (err) {
      error.className = 'error';
      error.textContent = `登录请求失败：${err.message}`;
    }
  });

  image.addEventListener('click', refreshCaptcha);
  refreshCaptcha();
})();
