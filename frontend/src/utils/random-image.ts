export const loginBackgroundImages = [
  'https://images.pexels.com/photos/4061395/pexels-photo-4061395.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=2'
];

export function getRandomLoginImage(): string {
  const randomIndex = Math.floor(Math.random() * loginBackgroundImages.length);
  return loginBackgroundImages[randomIndex];
}
