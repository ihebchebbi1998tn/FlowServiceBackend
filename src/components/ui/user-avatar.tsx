import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import defaultAvatar1 from '@/assets/default-avatar-1.png';
import defaultAvatar2 from '@/assets/default-avatar-2.png';
import defaultAvatar3 from '@/assets/default-avatar-3.png';

import { API_URL } from '@/config/api';

const DEFAULT_AVATARS = [defaultAvatar1, defaultAvatar2, defaultAvatar3];

/** Pick a consistent default avatar based on a seed (user id, name, etc.) */
export function getDefaultAvatar(seed: string | number): string {
  const hash = String(seed).split('').reduce((acc, c) => acc + c.charCodeAt(0), 0);
  return DEFAULT_AVATARS[hash % DEFAULT_AVATARS.length];
}

/** Resolve profile picture URL — handles relative paths and absolute URLs */
export function resolveProfilePicUrl(url?: string | null): string {
  if (!url) return '';
  if (url.startsWith('data:') || url.startsWith('blob:') || url.startsWith('http')) return url;
  // doc:{id} legacy pattern — convert to download URL
  if (url.startsWith('doc:')) {
    const docId = url.replace('doc:', '');
    return `${API_URL}/api/Documents/download/${docId}`;
  }
  // Relative path from backend
  return `${API_URL}/${url.replace(/^\//, '')}`;
}

interface UserAvatarProps {
  src?: string | null;
  name?: string;
  seed?: string | number;
  size?: 'xs' | 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeClasses = {
  xs: 'h-5 w-5',
  sm: 'h-7 w-7',
  md: 'h-8 w-8',
  lg: 'h-10 w-10',
};

const textSizes = {
  xs: 'text-[10px]',
  sm: 'text-xs',
  md: 'text-sm',
  lg: 'text-base',
};

export function UserAvatar({ src, name, seed, size = 'md', className = '' }: UserAvatarProps) {
  const resolvedUrl = resolveProfilePicUrl(src);
  const fallbackSeed = seed ?? name ?? 'user';
  const defaultImg = getDefaultAvatar(fallbackSeed);

  const initials = name
    ? name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
    : '?';

  return (
    <Avatar className={`${sizeClasses[size]} ${className}`}>
      {resolvedUrl ? (
        <AvatarImage
          src={resolvedUrl}
          alt={name || 'User'}
          className="object-cover"
          onError={(e) => {
            // On error, swap to default avatar
            (e.target as HTMLImageElement).src = defaultImg;
          }}
        />
      ) : (
        <AvatarImage src={defaultImg} alt={name || 'User'} className="object-cover" />
      )}
      <AvatarFallback className={`${textSizes[size]} bg-primary/10 text-primary font-medium`}>
        {initials}
      </AvatarFallback>
    </Avatar>
  );
}
