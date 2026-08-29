import { SecureTokenStorageService } from './secure-token-storage.service';

jest.mock('capacitor-secure-storage-plugin', () => ({
  SecureStoragePlugin: {
    get: jest.fn(),
    set: jest.fn(),
    remove: jest.fn(),
  },
}));

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { SecureStoragePlugin } = require('capacitor-secure-storage-plugin') as {
  SecureStoragePlugin: { get: jest.Mock; set: jest.Mock; remove: jest.Mock };
};

describe('SecureTokenStorageService', () => {
  let service: SecureTokenStorageService;

  beforeEach(() => {
    jest.clearAllMocks();
    SecureStoragePlugin.set.mockResolvedValue({ value: true });
    SecureStoragePlugin.remove.mockResolvedValue({ value: true });
    service = new SecureTokenStorageService();
  });

  it('keeps the access token in memory only, never touching secure storage', () => {
    service.setAccessToken('access-123');

    expect(service.getAccessToken()).toBe('access-123');
    expect(SecureStoragePlugin.set).not.toHaveBeenCalled();
  });

  it('hydrates the refresh token and device id from secure storage on bootstrap', async () => {
    SecureStoragePlugin.get.mockImplementation(({ key }: { key: string }) =>
      key === 'vespera.refreshToken' ? Promise.resolve({ value: 'stored-refresh' }) : Promise.resolve({ value: 'stored-device-id' }),
    );

    await service.hydrate();

    expect(service.getRefreshToken()).toBe('stored-refresh');
    expect(service.getOrCreateDeviceId()).toBe('stored-device-id');
    expect(SecureStoragePlugin.set).not.toHaveBeenCalled();
  });

  it('hydrates to null when secure storage has nothing stored yet (plugin rejects on missing key)', async () => {
    SecureStoragePlugin.get.mockRejectedValue(new Error('key not found'));

    await service.hydrate();

    expect(service.getRefreshToken()).toBeNull();
  });

  it('generates and persists a new device id exactly once when none was hydrated', () => {
    const first = service.getOrCreateDeviceId();
    const second = service.getOrCreateDeviceId();

    expect(first).toBe(second);
    expect(SecureStoragePlugin.set).toHaveBeenCalledTimes(1);
    expect(SecureStoragePlugin.set).toHaveBeenCalledWith({ key: 'vespera.deviceId', value: first });
  });

  it('persists a new refresh token to secure storage and updates the in-memory mirror', () => {
    service.setRefreshToken('new-refresh');

    expect(service.getRefreshToken()).toBe('new-refresh');
    expect(SecureStoragePlugin.set).toHaveBeenCalledWith({ key: 'vespera.refreshToken', value: 'new-refresh' });
  });

  it('removes the refresh token from secure storage when set to null', () => {
    service.setRefreshToken('some-token');
    service.setRefreshToken(null);

    expect(service.getRefreshToken()).toBeNull();
    expect(SecureStoragePlugin.remove).toHaveBeenCalledWith({ key: 'vespera.refreshToken' });
  });

  it('clears the access token in memory and the refresh token from secure storage', () => {
    service.setAccessToken('access-123');
    service.setRefreshToken('some-token');

    service.clear();

    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(SecureStoragePlugin.remove).toHaveBeenCalledWith({ key: 'vespera.refreshToken' });
  });
});
