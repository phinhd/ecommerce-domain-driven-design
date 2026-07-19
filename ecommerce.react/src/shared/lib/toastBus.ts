type ToastType = 'error' | 'success';
type ToastHandler = (message: string, type?: ToastType) => void;

let toastHandler: ToastHandler | null = null;

export const registerToastHandler = (handler: ToastHandler) => {
  toastHandler = handler;
};

export const emitToast = (message: string, type: ToastType = 'error') => {
  if (toastHandler) {
    toastHandler(message, type);
  } else {
    console.warn('Toast handler not registered:', message);
  }
};