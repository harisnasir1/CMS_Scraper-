import { useEffect } from 'react';

export function usePageTitle(title: string) {
  useEffect(() => {
    // Save the original title
    const originalTitle = document.title;
    
    // Set the new title
    document.title = title ? `${title} | Resellers Room` : 'Resellers Room';
    
    // Cleanup - restore the original title when component unmounts
    return () => {
      document.title = originalTitle;
    };
  }, [title]);
}
